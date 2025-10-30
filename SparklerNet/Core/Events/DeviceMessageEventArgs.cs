using MQTTnet;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Model;

// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SparklerNet.Core.Events;

/// <summary>
///     Event arguments for Sparkplug Device message received events.
/// </summary>
/// <param name="version">The Sparkplug specification version</param>
/// <param name="messageType">The Sparkplug message type</param>
/// <param name="groupId">The Group ID</param>
/// <param name="edgeNodeId">The Edge Node ID</param>
/// <param name="deviceId">The Device ID</param>
/// <param name="payload">The payload of the message</param>
/// <param name="mqttEventArgs">The original MQTT message received event arguments</param>
public sealed class DeviceMessageEventArgs(
    SparkplugVersion version,
    SparkplugMessageType messageType,
    string groupId,
    string edgeNodeId,
    string deviceId,
    Payload payload,
    MqttApplicationMessageReceivedEventArgs mqttEventArgs)
    : EventArgs
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
    ///     The Device ID
    /// </summary>
    public string DeviceId { get; init; } = deviceId;

    /// <summary>
    ///     The payload of the message
    /// </summary>
    public Payload Payload { get; init; } = payload;

    /// <summary>
    ///     The original MQTT message received event arguments
    /// </summary>
    public MqttApplicationMessageReceivedEventArgs MqttEventArgs { get; init; } = mqttEventArgs;
}