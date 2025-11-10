using MQTTnet;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Model;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SparklerNet.Core.Events;

/// <summary>
///     Represents a message context containing all necessary information for message processing and caching
/// </summary>
/// <param name="version">The Sparkplug specification version</param>
/// <param name="messageType">The Sparkplug message type</param>
/// <param name="groupId">The Group ID</param>
/// <param name="edgeNodeId">The Edge Node ID</param>
/// <param name="deviceId">The Device ID (optional)</param>
/// <param name="payload">The message payload</param>
/// <param name="eventArgs">The original MQTT message received event arguments</param>
/// <param name="isSeqConsecutive">Indicates whether the message sequence number is consecutive</param>
/// <param name="isCached">Indicates whether the message is cached</param>
/// <param name="timestamp">The timestamp when the message was received on the application layer in milliseconds</param>
public class SparkplugMessageEventArgs(
    SparkplugVersion version,
    SparkplugMessageType messageType,
    string groupId,
    string edgeNodeId,
    string? deviceId,
    Payload payload,
    MqttApplicationMessageReceivedEventArgs eventArgs,
    bool isSeqConsecutive = true,
    bool isCached = false,
    long timestamp = 0) : EventArgs
{
    /// <summary>
    ///     The Sparkplug specification version
    /// </summary>
    public SparkplugVersion Version { get; init; } = version;

    /// <summary>
    ///     The Sparkplug message type
    /// </summary>
    public SparkplugMessageType MessageType { get; init; } = messageType;

    /// <summary>
    ///     The Group ID
    /// </summary>
    public string GroupId { get; init; } = groupId;

    /// <summary>
    ///     The Edge Node ID
    /// </summary>
    public string EdgeNodeId { get; init; } = edgeNodeId;

    /// <summary>
    ///     The Device ID (optional)
    /// </summary>
    public string? DeviceId { get; init; } = deviceId;

    /// <summary>
    ///     The message payload
    /// </summary>
    public Payload Payload { get; init; } = payload;

    /// <summary>
    ///     The original MQTT message received event arguments
    /// </summary>
    public MqttApplicationMessageReceivedEventArgs EventArgs { get; init; } = eventArgs;

    /// <summary>
    ///     Indicates whether the message sequence number is consecutive to the expected sequence number
    /// </summary>
    public bool IsSeqConsecutive { get; set; } = isSeqConsecutive;

    /// <summary>
    ///     Indicates whether the message is cached
    /// </summary>
    public bool IsCached { get; set; } = isCached;

    /// <summary>
    ///     The timestamp when the message was received on the application layer in milliseconds
    /// </summary>
    public long Timestamp { get; init; } = timestamp == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : timestamp;
}