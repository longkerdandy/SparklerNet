using MQTTnet;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Model;

// ReSharper disable UnusedAutoPropertyAccessor.Global

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
public class SparkplugMessageEventArgs(
    SparkplugVersion version,
    SparkplugMessageType messageType,
    string groupId,
    string edgeNodeId,
    string? deviceId,
    Payload payload,
    MqttApplicationMessageReceivedEventArgs eventArgs) : EventArgs
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
}