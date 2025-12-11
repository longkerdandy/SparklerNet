namespace SparklerNet.HostApplication.Caches;

/// <summary>
///     Provides methods to track and query the status of edge nodes and devices.
/// </summary>
public interface IStatusTrackingService
{
    /// <summary>
    ///     Determines if a specific endpoint (EdgeNode or Device) is currently online.
    /// </summary>
    /// <param name="groupId">The group ID</param>
    /// <param name="edgeNodeId">The edge node ID</param>
    /// <param name="deviceId">The device ID (optional)</param>
    /// <returns>True if the endpoint is online, otherwise false.</returns>
    Task<bool> IsEndpointOnline(string groupId, string edgeNodeId, string? deviceId);

    /// <summary>
    ///     Updates the online status of a specific edge node.
    /// </summary>
    /// <param name="groupId">The group ID</param>
    /// <param name="edgeNodeId">The edge node ID</param>
    /// <param name="isOnline">True if the edge node is online, otherwise false.</param>
    /// <param name="bdSeq">The bdSeq metric value of Birth or Death certificates</param>
    /// <param name="timestamp">The timestamp of Birth or Death certificates</param>
    Task UpdateEdgeNodeOnlineStatus(string groupId, string edgeNodeId, bool isOnline, int bdSeq, long timestamp);

    /// <summary>
    ///     Updates the online status of a specific device.
    /// </summary>
    /// <param name="groupId">The group ID</param>
    /// <param name="edgeNodeId">The edge node ID</param>
    /// <param name="deviceId">The device ID</param>
    /// <param name="isOnline">True if the device is online, otherwise false.</param>
    /// <param name="timestamp">The timestamp of Birth or Death certificates</param>
    Task UpdateDeviceOnlineStatus(string groupId, string edgeNodeId, string deviceId, bool isOnline, long timestamp);

    /// <summary>
    ///     Clears all status-related cache entries.
    /// </summary>
    Task ClearCacheAsync();
}