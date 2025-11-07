using MQTTnet;
using SparklerNet.Core.Constants;

namespace SparklerNet.Core.Model;

/// <summary>
///     Represents a message context containing all necessary information for message processing and caching
/// </summary>
public record MessageContext
{
    /// <summary>
    ///     The Sparkplug specification version
    /// </summary>
    public SparkplugVersion Version { get; init; }

    /// <summary>
    ///     The Sparkplug message type
    /// </summary>
    public SparkplugMessageType MessageType { get; init; }

    /// <summary>
    ///     The Group ID
    /// </summary>
    public required string GroupId { get; init; }

    /// <summary>
    ///     The Edge Node ID
    /// </summary>
    public required string EdgeNodeId { get; init; }

    /// <summary>
    ///     The Device ID (optional)
    /// </summary>
    public string? DeviceId { get; init; }

    /// <summary>
    ///     The message payload
    /// </summary>
    public required Payload Payload { get; init; }

    /// <summary>
    ///     The original MQTT message received event arguments
    /// </summary>
    public required MqttApplicationMessageReceivedEventArgs EventArgs { get; init; }
}