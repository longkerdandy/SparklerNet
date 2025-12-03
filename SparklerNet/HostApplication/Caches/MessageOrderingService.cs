using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SparklerNet.Core.Events;
using SparklerNet.Core.Extensions;
using SparklerNet.Core.Options;
using static SparklerNet.Core.Constants.SparkplugMessageType;

namespace SparklerNet.HostApplication.Caches;

/// <summary>
///     Manages message ordering by caching and validating sequence numbers.
///     Ensures NDATA, DDATA, DBIRTH, and DDEATH messages are processed sequentially per Sparkplug specification.
///     Uses HybridCache for efficient in-memory and distributed caching of sequence states and pending messages.
/// </summary>
public class MessageOrderingService : IMessageOrderingService
{
    private const string SequenceKeyPrefix = "sparkplug:seq:"; // Prefix for the sequence number cache keys
    private const string PendingKeyPrefix = "sparkplug:pending:"; // Prefix for the pending messages cache keys
    private const string OrderingTag = "sparkplug:tags:ordering"; // Global tag for all message ordering cache entries
    private const int SequenceNumberRange = 256; // Valid sequence number range (0-255) as defined in Sparkplug spec
    private readonly HybridCache _cache; // Hybrid cache for storing sequence states and pending messages
    private readonly ILogger<MessageOrderingService> _logger; // Logger for the service
    private readonly SparkplugClientOptions _options; // Configuration options for the service

    // Timer collection for handling message reordering timeouts
    private readonly ConcurrentDictionary<string, Timer> _reorderTimers = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageOrderingService" />
    /// </summary>
    /// <param name="cache">The hybrid cache instance for storing sequence states and pending messages</param>
    /// <param name="options">The Sparkplug client options containing reordering configuration</param>
    /// <param name="loggerFactory">The logger factory to create the logger</param>
    public MessageOrderingService(HybridCache cache, SparkplugClientOptions options, ILoggerFactory loggerFactory)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = loggerFactory.CreateLogger<MessageOrderingService>() ??
                  throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public RebirthRequestCallback? OnRebirthRequested { get; set; }

    /// <inheritdoc />
    public PendingMessagesCallback? OnPendingMessages { get; set; }

    /// <inheritdoc />
    public async Task<List<SparkplugMessageEventArgs>> ProcessMessageOrderAsync(SparkplugMessageEventArgs message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.MessageType is not NDATA and not DDATA and not DBIRTH and not DDEATH)
            throw new ArgumentException($"Invalid message type '{message.MessageType}'.", nameof(message));

        // Validate sequence number is within the allowed range
        // If the sequence is invalid, return the message as-is to avoid processing it further
        if (message.Payload.Seq is < 0 or >= SequenceNumberRange) return [message];

        var result = new List<SparkplugMessageEventArgs>();

        // Use SemaphoreSlim for async thread safety
        var semaphore = CacheHelper.GetSemaphore(message.GroupId, message.EdgeNodeId, null);
        await semaphore.WaitAsync();

        try
        {
            // Check if the sequence is continuous with the last processed message
            if (await UpdateSequenceNumberAsync(message))
            {
                // If the sequence is continuous, add the current message to results
                result.Add(message);

                // Get and process any now-continuous pending messages
                var pendingMessages = await GetPendingMessagesAsync(message.GroupId, message.EdgeNodeId,
                    message.Payload.Seq);
                if (pendingMessages.Count > 0) result.AddRange(pendingMessages);
            }
            else
            {
                // If the sequence has a gap, cache the message for later processing
                // Return the cached message if it exists, this indicates a duplicate sequence number
                var oldMessage = await CachePendingMessageAsync(message);
                if (oldMessage != null) result.Add(oldMessage);
            }
        }
        finally
        {
            semaphore.Release();
        }

        return result;
    }

    /// <inheritdoc />
    public async Task ResetMessageOrderAsync(string groupId, string edgeNodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(edgeNodeId);

        // Build all required cache keys
        var seqKey = CacheHelper.BuildCacheKey(SequenceKeyPrefix, groupId, edgeNodeId, null);
        var pendingKey = CacheHelper.BuildCacheKey(PendingKeyPrefix, groupId, edgeNodeId, null);
        var timerKey = CacheHelper.BuildCacheKey(null, groupId, edgeNodeId, null);

        // Use SemaphoreSlim for async thread safety
        var semaphore = CacheHelper.GetSemaphore(groupId, edgeNodeId, null);
        await semaphore.WaitAsync();

        try
        {
            // Remove cached items
            await _cache.RemoveAsync(seqKey);
            await _cache.RemoveAsync(pendingKey);

            // Dispose timer if it exists
            if (_reorderTimers.TryRemove(timerKey, out var timer)) await timer.DisposeAsync();
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearCacheAsync()
    {
        // Clear all message ordering related cache entries using the global tag
        await _cache.RemoveByTagAsync(OrderingTag);

        // Dispose all reorder timers to prevent memory leaks
        foreach (var timer in _reorderTimers.Values) await timer.DisposeAsync();
        _reorderTimers.Clear();
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

        // Use SemaphoreSlim for async thread safety
        // Because the NDATA DDATA DBIRTH and DDEATH messages will share the same sequence number, the lock will be acquired on the EdgeNode level
        var semaphore = CacheHelper.GetSemaphore(groupId, edgeNodeId, null);
        await semaphore.WaitAsync();

        try
        {
            // Always remove and dispose the timer to prevent duplicate callbacks
            if (_reorderTimers.TryRemove(timerKey, out var timer)) await timer.DisposeAsync();

            // Pass seq as -1 to indicate timeout scenario
            pendingMessages = await GetPendingMessagesAsync(groupId, edgeNodeId, -1);
        }
        finally
        {
            semaphore.Release();
        }

        // Call the pending messages delegate outside the semaphore to avoid deadlocks
        if (pendingMessages.Count > 0 && OnPendingMessages != null)
        {
            _logger.LogDebug(
                "Reorder timeout has been triggered, {Count} pending messages will be processed: Group={Group}, Node={Node}, Device={Device}",
                pendingMessages.Count, groupId, edgeNodeId, deviceId ?? "<none>");
            await OnPendingMessages.Invoke(pendingMessages);
        }

        // Send the rebirth request if the option is enabled
        if (_options.SendRebirthWhenTimeout && OnRebirthRequested != null)
            await OnRebirthRequested.Invoke(groupId, edgeNodeId);
    }

    /// <summary>
    ///     Updates the sequence number in the cache if it represents a continuous sequence
    /// </summary>
    /// <param name="message">The message context containing sequence information</param>
    /// <returns>True if the sequence is continuous, false if there's a gap</returns>
    private async Task<bool> UpdateSequenceNumberAsync(SparkplugMessageEventArgs message)
    {
        // Build cache key for the sequence number tracking
        var cacheKey = CacheHelper.BuildCacheKey(SequenceKeyPrefix, message.GroupId, message.EdgeNodeId, null);

        // Check if the current sequence is continuous with the previously recorded sequence
        var previousSeq = await _cache.GetOrCreateAsync(cacheKey, _ => ValueTask.FromResult(-1), tags: [OrderingTag]);
        if (previousSeq != -1)
        {
            // Calculate the next expected sequence number with wrap-around
            var expectedNextSeq = (previousSeq + 1) % SequenceNumberRange;
            // If the current sequence doesn't match expected, there's a gap
            if (message.Payload.Seq != expectedNextSeq) return false;
        }

        // Update the cache with the current sequence number
        // If configured, also set an expiration for the sequence number cache entry
        await _cache.SetAsync(cacheKey, message.Payload.Seq, CreateSequenceCacheEntryOptions(), [OrderingTag]);
        return true;
    }

    /// <summary>
    ///     Caches a pending message that arrived out of order for later processing
    /// </summary>
    /// <param name="message">The message context containing sequence number and message data</param>
    /// <returns>The cached message if it exists (duplicated sequence number), otherwise null</returns>
    private async Task<SparkplugMessageEventArgs?> CachePendingMessageAsync(SparkplugMessageEventArgs message)
    {
        // Build cache key for pending messages
        var pendingKey = CacheHelper.BuildCacheKey(PendingKeyPrefix, message.GroupId, message.EdgeNodeId, null);

        // Get existing pending messages or create a new sorted collection with circular sequence ordering
        var pendingMessages = await _cache.GetOrCreateAsync(pendingKey,
            _ => ValueTask.FromResult(
                new SortedDictionary<int, SparkplugMessageEventArgs>(new CircularSequenceComparer())),
            tags: [OrderingTag]);

        // Add or update the message with this sequence number
        // If the sequence number already exists, the existing message will be returned
        message.IsCached = true; // Mark the message as cached
        pendingMessages.TryReplace(message.Payload.Seq, message, out var result);

        // Cache the updated pending messages
        await _cache.SetAsync(pendingKey, pendingMessages, tags: [OrderingTag]);
        _logger.LogDebug(
            "{MessageType} message cached due to sequence gap: Group={Group}, Node={Node}, Device={Device}, Seq={Seq}",
            message.MessageType, message.GroupId, message.EdgeNodeId, message.DeviceId ?? "<none>",
            message.Payload.Seq);

        // Build timer key for managing reordering timeout
        var timerKey = CacheHelper.BuildCacheKey(null, message.GroupId, message.EdgeNodeId, null);

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
    /// <param name="seq">
    ///     The current sequence number, -1 for the reorder timeout scenario (still processes consecutive sequences in order)
    /// </param>
    /// <returns>List of pending messages that can now be processed in order</returns>
    [SuppressMessage("ReSharper", "InvertIf")]
    private async Task<List<SparkplugMessageEventArgs>> GetPendingMessagesAsync(string groupId, string edgeNodeId,
        int seq)
    {
        // Validate required parameters
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(edgeNodeId);

        var result = new List<SparkplugMessageEventArgs>();

        // Build all required cache keys
        var seqKey = CacheHelper.BuildCacheKey(SequenceKeyPrefix, groupId, edgeNodeId, null);
        var pendingKey = CacheHelper.BuildCacheKey(PendingKeyPrefix, groupId, edgeNodeId, null);
        var timerKey = CacheHelper.BuildCacheKey(null, groupId, edgeNodeId, null);

        // Get existing pending messages
        var pendingMessages = await _cache.GetOrCreateAsync(pendingKey,
            _ => ValueTask.FromResult<SortedDictionary<int, SparkplugMessageEventArgs>?>(null),
            tags: [OrderingTag]);

        // If no pending messages, remove the cache entry and return an empty result
        if (pendingMessages == null || pendingMessages.Count == 0)
        {
            await _cache.RemoveAsync(pendingKey);
            return result;
        }

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
        // If configured, also set an expiration for the sequence number cache entry
        await _cache.SetAsync(seqKey, currentSeq, CreateSequenceCacheEntryOptions(), tags: [OrderingTag]);

        // Update or remove pending messages cache and handle timer accordingly
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (pendingMessages.Count > 0 && result.Count > 0)
        {
            // Still have pending messages and size changed, update cache and reset timer
            await _cache.SetAsync(pendingKey, pendingMessages, tags: [OrderingTag]);
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
            // No more pending messages, clean up the cache and timer
            await _cache.RemoveAsync(pendingKey);
            if (_reorderTimers.TryRemove(timerKey, out var timer)) await timer.DisposeAsync();
        }

        return result;
    }

    /// <summary>
    ///     Creates cache entry options for sequence number caching with appropriate expiration settings
    /// </summary>
    /// <returns>Configured HybridCacheEntryOptions with expiration if specified in options</returns>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    protected internal HybridCacheEntryOptions CreateSequenceCacheEntryOptions()
    {
        if (_options.SeqCacheExpiration > 0)
            // Set expiration based on the configuration using object initializer
            return new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(_options.SeqCacheExpiration)
            };

        return new HybridCacheEntryOptions();
    }

    /// <summary>
    ///     Custom comparer for circular sequence numbers (0-255)
    ///     Ensures proper ordering when sequence numbers wrap around (0 is considered greater than 255)
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    protected internal class CircularSequenceComparer : IComparer<int>
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
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (x < 32 && y > 223) return 1;
            if (x > 223 && y < 32) return -1;

            // Otherwise, use normal integer comparison
            return x.CompareTo(y);
        }
    }
}