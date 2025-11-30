using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Hybrid;

namespace SparklerNet.HostApplication.Caches;

/// <summary>
///     Service responsible for tracking the online status of edge nodes and devices
///     Provides methods to check and update the online status of specific edge nodes and devices
///     Uses HybridCache for efficient caching with both in-memory and distributed capabilities
/// </summary>
public class StatusTrackingService : IStatusTrackingService
{
    private const string StatusKeyPrefix = "sparkplug:status:"; // Prefix for the status cache keys
    private readonly HybridCache _cache;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StatusTrackingService" />
    /// </summary>
    /// <param name="cache">The HybridCache instance for caching online status</param>
    public StatusTrackingService(HybridCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public async Task<bool> IsOnline(string groupId, string edgeNodeId, string? deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(edgeNodeId);

        // Build the cache key for status tracking
        var cacheKey = CacheHelper.BuildCacheKey(StatusKeyPrefix, groupId, edgeNodeId, deviceId);

        // Get the status from the cache or create a new entry if it doesn't exist
        var status = await _cache.GetOrCreateAsync<EndpointStatus?>(
            cacheKey, _ => ValueTask.FromResult<EndpointStatus?>(null));

        // If the status is not in the cache, assume it is offline
        return status is { IsOnline: true };
    }

    /// <inheritdoc />
    public async Task UpdateEdgeNodeOnlineStatus(string groupId, string edgeNodeId, bool isOnline, int bdSeq,
        long timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(edgeNodeId);

        // Build the cache key for status tracking
        var cacheKey = CacheHelper.BuildCacheKey(StatusKeyPrefix, groupId, edgeNodeId, null);
        var cacheTag = CacheHelper.BuildCacheKey(null, groupId, edgeNodeId, null);

        // Create a new status object
        var newStatus = new EndpointStatus { IsOnline = isOnline, BdSeq = bdSeq, Timestamp = timestamp };

        // Use SemaphoreSlim for async thread safety
        var semaphore = CacheHelper.GetSemaphore(groupId, edgeNodeId, null);

        try
        {
            // Wait for the semaphore asynchronously
            await semaphore.WaitAsync();

            // Get the current status from the cache or create a new entry if it doesn't exist
            var currentStatus = await _cache.GetOrCreateAsync(
                cacheKey, _ => ValueTask.FromResult(newStatus), tags: [cacheTag]);

            // Online status update logic
            if (newStatus.IsOnline)
            {
                // Update the cache if the new status is newer than the current status
                // Note: if the cache is empty, currentStatus will be set to newStatus by GetOrCreateAsync
                if (newStatus.Timestamp > currentStatus.Timestamp)
                    await _cache.SetAsync(cacheKey, newStatus, tags: [cacheTag]);
            }
            // Offline status update logic
            else
            {
                // When the current status is offline, update if the new status is newer than the current status
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (!currentStatus.IsOnline && newStatus.Timestamp > currentStatus.Timestamp)
                    await _cache.SetAsync(cacheKey, newStatus, tags: [cacheTag]);

                // When the current status is online, update if:
                // 1. The new status has the same bdSeq 
                // 2. The new status has the same or newer timestamp
                if (currentStatus.IsOnline &&
                    (newStatus.BdSeq == currentStatus.BdSeq || newStatus.Timestamp >= currentStatus.Timestamp))
                {
                    await _cache.RemoveByTagAsync(cacheTag);
                    await _cache.SetAsync(cacheKey, newStatus, tags: [cacheTag]);
                }
            }
        }
        finally
        {
            // Always release the semaphore to prevent deadlocks
            semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateDeviceOnlineStatus(string groupId, string edgeNodeId, string deviceId, bool isOnline,
        long timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(edgeNodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        // Build the cache key for status tracking
        var cacheKey = CacheHelper.BuildCacheKey(StatusKeyPrefix, groupId, edgeNodeId, deviceId);
        var cacheTag = CacheHelper.BuildCacheKey(null, groupId, edgeNodeId, null);

        // Create a new status object
        var newStatus = new EndpointStatus { IsOnline = isOnline, BdSeq = 0, Timestamp = timestamp };

        // Use SemaphoreSlim for async thread safety
        var semaphore = CacheHelper.GetSemaphore(groupId, edgeNodeId, deviceId);

        try
        {
            // Wait for the semaphore asynchronously
            await semaphore.WaitAsync();

            // Get the current status from the cache or create a new entry if it doesn't exist
            var currentStatus = await _cache.GetOrCreateAsync(
                cacheKey, _ => ValueTask.FromResult(newStatus), tags: [cacheTag]);

            // Update the cache if the new status is newer than the current status
            // Note: if the cache is empty, currentStatus will be set to newStatus by GetOrCreateAsync
            if (newStatus.Timestamp > currentStatus.Timestamp)
                await _cache.SetAsync(cacheKey, newStatus, tags: [cacheTag]);
        }
        finally
        {
            // Always release the semaphore to prevent deadlocks
            semaphore.Release();
        }
    }

    /// <summary>
    ///     Simple data class to store online status information
    ///     Allows for future expansion of status data beyond just a boolean flag
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    protected record EndpointStatus
    {
        public bool IsOnline { get; init; }

        public int BdSeq { get; init; }

        public long Timestamp { get; init; }
    }
}