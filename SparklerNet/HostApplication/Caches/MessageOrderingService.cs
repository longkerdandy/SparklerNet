using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SparklerNet.Core.Events;
using SparklerNet.Core.Extensions;
using SparklerNet.Core.Options;

namespace SparklerNet.HostApplication.Caches;

/// <summary>
///     Service responsible for managing message ordering by caching and validating sequence numbers
///     Ensures messages are processed in sequential order according to the Sparkplug specification
/// </summary>
public class MessageOrderingService : IMessageOrderingService
{
    private const string SequenceKeyPrefix = "sparkplug:seq:"; // Prefix for the sequence number cache keys
    private const string PendingKeyPrefix = "sparkplug:pending:"; // Prefix for the pending messages cache keys
    private const int SequenceNumberRange = 256; // Valid sequence number range (0-255) as defined in Sparkplug spec
    private readonly IMemoryCache _cache; // In-memory cache for storing sequence states and pending messages

    // Collection to track all cache keys created by this service
    private readonly ConcurrentDictionary<string, object?> _cachedPendingKeys = new();
    private readonly ConcurrentDictionary<string, object?> _cachedSeqKeys = new();

    private readonly ILogger<MessageOrderingService> _logger; // Logger for the service
    private readonly SparkplugClientOptions _options; // Configuration options for the service

    // Fine-grained locks for thread-safe operations on specific devices
    private readonly ConcurrentDictionary<string, object> _reorderLocks = new();

    // Timer collection for handling message reordering timeouts
    private readonly ConcurrentDictionary<string, Timer> _reorderTimers = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageOrderingService" />
    /// </summary>
    /// <param name="cache">The memory cache instance for storing sequence states and pending messages</param>
    /// <param name="options">The Sparkplug client options containing reordering configuration</param>
    /// <param name="loggerFactory">The logger factory to create the logger</param>
    public MessageOrderingService(IMemoryCache cache, SparkplugClientOptions options, ILoggerFactory loggerFactory)
    {
        _cache = cache;
        _options = options;
        _logger = loggerFactory.CreateLogger<MessageOrderingService>();
    }

    /// <summary>
    ///     Gets or sets the delegate to be called when a Rebirth message needs to be sent due to detected message gaps
    /// </summary>
    public RebirthRequestCallback? OnRebirthRequested { get; set; }

    /// <summary>
    ///     Gets or sets the delegate to be called when pending messages have been processed and are ready for consumption
    /// </summary>
    public PendingMessagesCallback? OnPendingMessages { get; set; }

    /// <inheritdoc />
    public List<SparkplugMessageEventArgs> ProcessMessageOrder(SparkplugMessageEventArgs message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Validate sequence number is within the allowed range
        // If the sequence is invalid, return the message as-is to avoid processing it further
        if (message.Payload.Seq is < 0 or >= SequenceNumberRange) return [message];

        var result = new List<SparkplugMessageEventArgs>();
        // Use fine-grained lock to ensure thread safety for this specific device/node
        lock (GetLockObject(message.GroupId, message.EdgeNodeId, message.DeviceId))
        {
            // Check if the sequence is continuous with the last processed message
            if (UpdateSequenceNumber(message))
            {
                // If the sequence is continuous, add the current message to results
                result.Add(message);

                // Get and process any now-continuous pending messages
                var pendingMessages = GetPendingMessages(message.GroupId, message.EdgeNodeId,
                    message.DeviceId,
                    message.Payload.Seq);
                if (pendingMessages.Count > 0) result.AddRange(pendingMessages);
            }
            else
            {
                // If the sequence has a gap, cache the message for later processing
                // Return the cached message if it exists, this indicates a duplicate sequence number
                var oldMessage = CachePendingMessage(message);
                if (oldMessage != null) result.Add(oldMessage);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public void ClearMessageOrder(string groupId, string edgeNodeId, string? deviceId)
    {
        // Build all required cache keys
        var seqKey = BuildCacheKey(SequenceKeyPrefix, groupId, edgeNodeId, deviceId);
        var pendingKey = BuildCacheKey(PendingKeyPrefix, groupId, edgeNodeId, deviceId);
        var timerKey = BuildCacheKey(null, groupId, edgeNodeId, deviceId);

        // Use lock to ensure thread safety during cache and timer cleanup
        lock (GetLockObject(groupId, edgeNodeId, deviceId))
        {
            // Remove cached items and dispose timer if it exists
            _cache.Remove(seqKey);
            _cache.Remove(pendingKey);
            _cachedSeqKeys.TryRemove(seqKey, out _);
            _cachedPendingKeys.TryRemove(pendingKey, out _);
            if (_reorderTimers.TryRemove(timerKey, out var timer))
                timer.Dispose();
        }
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    public List<SparkplugMessageEventArgs> GetAllMessagesAndClearCache()
    {
        // Dispose all reorder timers to prevent callbacks during cache clearing
        var timerKeys = _reorderTimers.Keys.ToList();
        foreach (var timerKey in timerKeys)
            if (_reorderTimers.TryRemove(timerKey, out var timer))
                timer.Dispose();

        // Get all pending keys from the cache, clear the pending messages cache
        var result = new List<SparkplugMessageEventArgs>();
        var pendingKeys = _cachedPendingKeys.Keys.ToList();
        foreach (var pendingKey in pendingKeys)
        {
            // Get pending messages from the cache if available
            if (!_cache.TryGetValue(pendingKey, out SortedDictionary<int, SparkplugMessageEventArgs>? pendingMessages)
                || pendingMessages == null) continue;

            // Add all pending messages to the result list
            result.AddRange(pendingMessages.Values);

            // Remove the pending messages from the cache
            _cache.Remove(pendingKey);
        }

        // Clear the pending keys cache
        _cachedPendingKeys.Clear();

        // Clear the sequence number cache and the sequence keys cache
        var seqKeys = _cachedSeqKeys.Keys.ToList();
        foreach (var seqKey in seqKeys) _cache.Remove(seqKey);
        _cachedSeqKeys.Clear();

        return result;
    }

    /// <summary>
    ///     Handles message reordering timeout events, triggered when a gap in sequence numbers persists beyond the configured
    ///     timeout
    /// </summary>
    /// <param name="state">The timer key that identifies the edge node/device combination</param>
    // ReSharper disable once AsyncVoidMethod Required for the timer callback pattern
    private async void OnReorderTimeout(object? state)
    {
        if (state is not string timerKey) return;

        // Parse the timer key to extract groupId, edgeNodeId, and deviceId
        // The timer key format is "groupId:edgeNodeId:deviceId" or "groupId:edgeNodeId"
        var parts = timerKey.Split(':');
        if (parts.Length < 2) return;
        var groupId = parts[0];
        var edgeNodeId = parts[1];
        var deviceId = parts.Length > 2 ? parts[2] : null;

        List<SparkplugMessageEventArgs> pendingMessages;

        // Use lock to ensure thread safety and prevent race conditions with concurrent message processing
        lock (GetLockObject(groupId, edgeNodeId, deviceId))
        {
            // Always remove and dispose the timer to prevent duplicate callbacks
            if (_reorderTimers.TryRemove(timerKey, out var timer)) timer.Dispose();

            // Pass seq as -1 to indicate timeout scenario
            pendingMessages = GetPendingMessages(groupId, edgeNodeId, deviceId, -1);
        }

        // Call the pending messages delegate outside the lock to avoid deadlocks with async operations
        if (pendingMessages.Count > 0 && OnPendingMessages != null)
        {
            _logger.LogDebug(
                "Reorder timeout has been triggered, {Count} pending messages will be processed: Group={Group}, Node={Node}, Device={Device}",
                pendingMessages.Count, groupId, edgeNodeId, deviceId ?? "<none>");
            await OnPendingMessages.Invoke(pendingMessages);
        }

        // Send the rebirth request if configured and delegate is set
        if (_options.SendRebirthWhenTimeout && OnRebirthRequested != null)
            await OnRebirthRequested.Invoke(groupId, edgeNodeId, deviceId);
    }

    /// <summary>
    ///     Builds a standardized cache key based on the provided prefix and identifiers
    /// </summary>
    /// <param name="prefix">The prefix to use for the key (can be null)</param>
    /// <param name="groupId">The group ID part of the key</param>
    /// <param name="edgeNodeId">The edge node ID part of the key</param>
    /// <param name="deviceId">The device ID part of the key (optional)</param>
    /// <returns>The constructed cache key in format "prefix:groupId:edgeNodeId:deviceId" or "prefix:groupId:edgeNodeId"</returns>
    internal static string BuildCacheKey(string? prefix, string groupId, string edgeNodeId, string? deviceId)
    {
        var baseKey = !string.IsNullOrEmpty(deviceId)
            ? $"{groupId}:{edgeNodeId}:{deviceId}"
            : $"{groupId}:{edgeNodeId}";

        return string.IsNullOrEmpty(prefix) ? baseKey : $"{prefix}{baseKey}";
    }

    /// <summary>
    ///     Gets a lock object for the specified context from the reorder locks dictionary
    ///     Ensures thread safety for operations on a specific device/node combination
    /// </summary>
    /// <param name="groupId">The group ID part of the key</param>
    /// <param name="edgeNodeId">The edge node ID part of the key</param>
    /// <param name="deviceId">The device ID part of the key (optional)</param>
    /// <returns>The lock object for the specified EdgeNode/Device</returns>
    private object GetLockObject(string groupId, string edgeNodeId, string? deviceId)
    {
        var key = BuildCacheKey(null, groupId, edgeNodeId, deviceId);
        return _reorderLocks.GetOrAdd(key, _ => new object());
    }

    /// <summary>
    ///     Updates the sequence number in the cache if it represents a continuous sequence
    /// </summary>
    /// <param name="message">The message context containing sequence information</param>
    /// <returns>True if the sequence is continuous, false if there's a gap</returns>
    private bool UpdateSequenceNumber(SparkplugMessageEventArgs message)
    {
        // Build cache key for the sequence number tracking
        var cacheKey = BuildCacheKey(SequenceKeyPrefix, message.GroupId, message.EdgeNodeId,
            message.DeviceId);

        // Check if the current sequence is continuous with the previously recorded sequence
        if (_cache.TryGetValue(cacheKey, out int previousSeq))
        {
            // Calculate the next expected sequence number with wrap-around
            var expectedNextSeq = (previousSeq + 1) % SequenceNumberRange;
            // If the current sequence doesn't match expected, there's a gap
            if (message.Payload.Seq != expectedNextSeq) return false;
        }

        // Update the cache with the current sequence number
        _cache.Set(cacheKey, message.Payload.Seq, CreateSequenceCacheEntryOptions());
        _cachedSeqKeys.TryAdd(cacheKey, null);

        return true;
    }

    /// <summary>
    ///     Caches a pending message that arrived out of order for later processing
    /// </summary>
    /// <param name="message">The message context containing sequence number and message data</param>
    /// <returns>The cached message if it exists (duplicated sequence number), otherwise null</returns>
    private SparkplugMessageEventArgs? CachePendingMessage(SparkplugMessageEventArgs message)
    {
        // Build cache key for pending messages
        var pendingKey = BuildCacheKey(PendingKeyPrefix, message.GroupId, message.EdgeNodeId,
            message.DeviceId);

        // Get existing pending messages or create a new sorted collection with circular sequence ordering
        var pendingMessages =
            _cache.TryGetValue(pendingKey, out SortedDictionary<int, SparkplugMessageEventArgs>? existingMessages)
                ? existingMessages ??
                  new SortedDictionary<int, SparkplugMessageEventArgs>(new CircularSequenceComparer())
                : new SortedDictionary<int, SparkplugMessageEventArgs>(new CircularSequenceComparer());

        // Add or update the message with this sequence number
        // If the sequence number already exists, the existing message will be returned
        message.IsCached = true; // Mark the message as cached
        pendingMessages.TryReplace(message.Payload.Seq, message, out var result);

        // Cache the updated pending messages
        _cache.Set(pendingKey, pendingMessages);
        _cachedPendingKeys.TryAdd(pendingKey, null);
        _logger.LogDebug(
            "{MessageType} message has been cached due to sequence disorder: Group={Group}, Node={Node}, Device={Device}, Seq={Seq}",
            message.MessageType, message.GroupId, message.EdgeNodeId, message.DeviceId ?? "<none>",
            message.Payload.Seq);

        // Build timer key for managing reordering timeout
        var timerKey = BuildCacheKey(null, message.GroupId, message.EdgeNodeId, message.DeviceId);

        // Check if the new message becomes the first message in the sorted collection
        var isFirstMessage = pendingMessages.First().Key == message.Payload.Seq;

        if (isFirstMessage)
            // If this is now the first message, reset the timeout timer
            _reorderTimers.AddOrUpdate(timerKey,
                _ => new Timer(OnReorderTimeout, timerKey, _options.SeqReorderTimeout, Timeout.Infinite),
                (_, existingTimer) =>
                {
                    existingTimer.Change(_options.SeqReorderTimeout, Timeout.Infinite);
                    return existingTimer;
                });
        else
            // Safety check: ensure we have a timer even if the new message is not the first one
            // This prevents pending messages from being stuck if the timer was lost due to concurrent operations
            _reorderTimers.TryAdd(timerKey,
                new Timer(OnReorderTimeout, timerKey, _options.SeqReorderTimeout, Timeout.Infinite));

        return result;
    }

    /// <summary>
    ///     Gets and processes any pending messages that now have a continuous sequence
    ///     When seq is -1 (timeout scenario), still processes consecutive message sequences in order
    /// </summary>
    /// <param name="groupId">The group ID</param>
    /// <param name="edgeNodeId">The edge node ID</param>
    /// <param name="deviceId">The device ID (optional)</param>
    /// <param name="seq">
    ///     The current sequence number, -1 for the reorder timeout scenario (still processes consecutive sequences in order)
    /// </param>
    /// <returns>List of pending messages that can now be processed in order</returns>
    [SuppressMessage("ReSharper", "InvertIf")]
    private List<SparkplugMessageEventArgs> GetPendingMessages(string groupId, string edgeNodeId, string? deviceId,
        int seq)
    {
        // Validate required parameters
        if (string.IsNullOrEmpty(groupId)) throw new ArgumentNullException(nameof(groupId));
        if (string.IsNullOrEmpty(edgeNodeId)) throw new ArgumentNullException(nameof(edgeNodeId));

        var result = new List<SparkplugMessageEventArgs>();

        // Build all required cache keys
        var seqKey = BuildCacheKey(SequenceKeyPrefix, groupId, edgeNodeId, deviceId);
        var pendingKey = BuildCacheKey(PendingKeyPrefix, groupId, edgeNodeId, deviceId);
        var timerKey = BuildCacheKey(null, groupId, edgeNodeId, deviceId);

        // Check if we have pending messages
        if (!_cache.TryGetValue(pendingKey, out SortedDictionary<int, SparkplugMessageEventArgs>? pendingMessages) ||
            pendingMessages == null || pendingMessages.Count == 0)
            return result;

        // Process pending messages in order until we find a gap
        // When seq is -1 (timeout), we still process consecutive message sequences in order
        var currentSeq = seq;
        var nextExpectedSeq = currentSeq < 0 ? -1 : (currentSeq + 1) % SequenceNumberRange;
        bool foundMoreMessages;

        do
        {
            foundMoreMessages = false;
            if (pendingMessages.Count > 0)
            {
                var firstKey = pendingMessages.Keys.First();

                // Process if it's the expected next sequence or if we're in timeout mode (-1)
                if (firstKey == nextExpectedSeq || nextExpectedSeq < 0)
                {
                    var message = pendingMessages[firstKey];
                    message.IsSeqConsecutive = nextExpectedSeq >= 0; // Check if the sequence is consecutive
                    result.Add(message);
                    pendingMessages.Remove(firstKey);
                    currentSeq = firstKey;
                    nextExpectedSeq = (currentSeq + 1) % SequenceNumberRange;
                    foundMoreMessages = true;
                }
            }
        } while (foundMoreMessages && pendingMessages.Count > 0);

        // Update the cache with the new sequence after processing all continuous messages
        _cache.Set(seqKey, currentSeq, CreateSequenceCacheEntryOptions());
        _cachedSeqKeys.TryAdd(seqKey, null);

        // Update or remove pending messages cache and handle timer accordingly
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (pendingMessages.Count > 0 && result.Count > 0)
        {
            // Still have pending messages and size changed, update cache and reset timer
            _cache.Set(pendingKey, pendingMessages);
            _reorderTimers.AddOrUpdate(timerKey,
                _ => new Timer(OnReorderTimeout, timerKey, _options.SeqReorderTimeout, Timeout.Infinite),
                (_, existingTimer) =>
                {
                    existingTimer.Change(_options.SeqReorderTimeout, Timeout.Infinite);
                    return existingTimer;
                });
        }
        else if (pendingMessages.Count == 0)
        {
            // No more pending messages, clean up cache and timer
            _cache.Remove(pendingKey);
            _cachedPendingKeys.TryRemove(pendingKey, out _);
            if (_reorderTimers.TryRemove(timerKey, out var timer))
                timer.Dispose();
        }

        return result;
    }

    /// <summary>
    ///     Creates cache entry options for sequence number caching with appropriate expiration settings
    /// </summary>
    /// <returns>Configured MemoryCacheEntryOptions with sliding expiration if specified in options</returns>
    private MemoryCacheEntryOptions CreateSequenceCacheEntryOptions()
    {
        var memoryCacheOptions = new MemoryCacheEntryOptions();

        if (_options.SeqCacheExpiration <= 0) return memoryCacheOptions;

        // Set sliding expiration based on the configuration
        memoryCacheOptions.SlidingExpiration = TimeSpan.FromMinutes(_options.SeqCacheExpiration);

        // Register eviction callback to remove the key from _cachedSeqKeys when cache entry is evicted
        memoryCacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
        {
            if (key is not string cacheKey) return;
            _cachedSeqKeys.TryRemove(cacheKey, out _);
        });

        return memoryCacheOptions;
    }

    /// <summary>
    ///     Custom comparer for circular sequence numbers (0-255)
    ///     Ensures proper ordering when sequence numbers wrap around (0 is considered greater than 255)
    /// </summary>
    internal class CircularSequenceComparer : IComparer<int>
    {
        /// <summary>
        ///     Compares two circular sequence numbers considering the wrap-around at 255->0
        /// </summary>
        /// <param name="x">First sequence number to compare</param>
        /// <param name="y">Second sequence number to compare</param>
        /// <returns>Comparison result based on circular sequence rules</returns>
        public int Compare(int x, int y)
        {
            // For circular sequence numbers (0-255), handle the wrap-around case
            // If x is near 0 (lower third) and y is near 255 (upper third), consider x > y
            // ReSharper disable once - ConvertIfStatementToSwitchStatement
            if (x < 32 && y > 223) return 1;
            if (x > 223 && y < 32) return -1;

            // Otherwise, use normal integer comparison
            return x.CompareTo(y);
        }
    }
}