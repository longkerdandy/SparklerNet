using System.Collections.Concurrent;

namespace SparklerNet.HostApplication.Caches;

/// <summary>
///     Provides helper methods for cache operations.
/// </summary>
public static class CacheHelper
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Semaphores = new();

    /// <summary>
    ///     Builds a standardized cache key based on the provided prefix and identifiers
    /// </summary>
    /// <param name="prefix">The prefix to use for the key (can be null)</param>
    /// <param name="groupId">The group ID part of the key</param>
    /// <param name="edgeNodeId">The edge node ID part of the key</param>
    /// <param name="deviceId">The device ID part of the key (optional)</param>
    /// <returns>The constructed cache key in format "prefix:groupId:edgeNodeId:deviceId" or "prefix:groupId:edgeNodeId"</returns>
    public static string BuildCacheKey(string? prefix, string groupId, string edgeNodeId, string? deviceId)
    {
        var baseKey = !string.IsNullOrEmpty(deviceId)
            ? $"{groupId}:{edgeNodeId}:{deviceId}"
            : $"{groupId}:{edgeNodeId}";

        return string.IsNullOrEmpty(prefix) ? baseKey : $"{prefix}{baseKey}";
    }

    /// <summary>
    ///     Gets a SemaphoreSlim object for the specified context to support async locking
    ///     Ensures thread safety for asynchronous operations on a specific device/node combination
    /// </summary>
    /// <param name="groupId">The group ID part of the key</param>
    /// <param name="edgeNodeId">The edge node ID part of the key</param>
    /// <param name="deviceId">The device ID part of the key (optional)</param>
    /// <returns>The SemaphoreSlim object for the specified EdgeNode/Device</returns>
    public static SemaphoreSlim GetSemaphore(string groupId, string edgeNodeId, string? deviceId)
    {
        var key = BuildCacheKey(null, groupId, edgeNodeId, deviceId);
        return Semaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>
    ///     Clears all SemaphoreSlim objects from the cache
    /// </summary>
    public static void ClearSemaphores()
    {
        foreach (var semaphore in Semaphores.Values) semaphore.Dispose();
        Semaphores.Clear();
    }
}