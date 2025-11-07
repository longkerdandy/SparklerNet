using SparklerNet.Core.Events;

namespace SparklerNet.HostApplication.Caches;

/// <summary>
///     Delegate for handling Rebirth requests when a message gap is detected or timeout occurs
/// </summary>
/// <param name="groupId">The group ID of the entity requiring rebirth</param>
/// <param name="edgeNodeId">The edge node ID of the entity requiring rebirth</param>
/// <param name="deviceId">The device ID of the entity requiring rebirth (optional)</param>
public delegate Task RebirthRequestCallback(string groupId, string edgeNodeId, string? deviceId = null);

/// <summary>
///     Delegate for notifying when pending messages have been processed and are ready for consumption
/// </summary>
/// <param name="messageContexts">The collection of pending messages that are now ready for processing</param>
public delegate Task PendingMessagesCallback(IEnumerable<SparkplugMessageEventArgs> messageContexts);

/// <summary>
///     Interface for a service responsible for managing message ordering by caching and validating sequence numbers.
///     Ensures messages are processed in sequential order according to the Sparkplug specification.
/// </summary>
public interface IMessageOrderingService
{
    /// <summary>
    ///     Gets or sets the delegate to be called when a Rebirth message needs to be sent due to detected message gaps
    /// </summary>
    RebirthRequestCallback? OnRebirthRequested { get; set; }

    /// <summary>
    ///     Gets or sets the delegate to be called when pending messages have been processed and are ready for consumption
    /// </summary>
    PendingMessagesCallback? OnPendingMessages { get; set; }

    /// <summary>
    ///     Processes a message in the correct order, handling both continuous and non-continuous sequences
    ///     Messages with continuous sequence numbers are processed immediately
    ///     Messages with gaps in sequence are cached for later processing when the gap is filled
    /// </summary>
    /// <param name="messageContext">The message context to process</param>
    /// <returns>List of messages that can be processed (current message if continuous and any continuous pending messages)</returns>
    List<SparkplugMessageEventArgs> ProcessMessageOrder(SparkplugMessageEventArgs messageContext);

    /// <summary>
    ///     Clears the sequence cache and pending messages for a specific edge node or device
    ///     Also cleans up any associated timer resources
    /// </summary>
    /// <param name="groupId">The group ID of the edge node</param>
    /// <param name="edgeNodeId">The edge node ID</param>
    /// <param name="deviceId">The device ID (optional)</param>
    void ClearMessageOrderCache(string groupId, string edgeNodeId, string? deviceId);
}