using MQTTnet;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Model;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global

namespace SparklerNet.Core.Events;

/// <summary>
///     Event arguments for Sparkplug Host Application message received events.
/// </summary>
/// <param name="version">The Sparkplug specification version</param>
/// <param name="messageType">The Sparkplug message type</param>
/// <param name="hostId">The Host Application ID</param>
/// <param name="payload">The payload of the message</param>
/// <param name="mqttEventArgs">The original MQTT message received event arguments</param>
public sealed class HostApplicationMessageEventArgs(
    SparkplugVersion version,
    SparkplugMessageType messageType,
    string hostId,
    StatePayload payload,
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
    ///     The Host Application ID
    /// </summary>
    public string HostId { get; init; } = hostId;

    /// <summary>
    ///     The payload of the message
    /// </summary>
    public StatePayload Payload { get; init; } = payload;

    /// <summary>
    ///     The original MQTT message received event arguments
    /// </summary>
    public MqttApplicationMessageReceivedEventArgs MqttEventArgs { get; init; } = mqttEventArgs;
}