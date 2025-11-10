using MQTTnet;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Model;

namespace SparklerNet.HostApplication.Extensions;

/// <summary>
///     Provides extension methods for <see cref="SparkplugHostApplication" /> to send Rebirth commands
/// </summary>
public static class SparkplugHostApplicationExtensions
{
    /// <summary>
    ///     Sends a Rebirth command to a specific Edge Node
    /// </summary>
    /// <param name="hostApplication">The Sparkplug Host Application instance</param>
    /// <param name="groupId">The Sparkplug Group ID</param>
    /// <param name="edgeNodeId">The Sparkplug Edge Node ID</param>
    /// <returns>The MQTT Client Publish Result</returns>
    public static Task<MqttClientPublishResult> PublishEdgeNodeRebirthCommandAsync(
        this SparkplugHostApplication hostApplication, string groupId, string edgeNodeId)
    {
        ArgumentNullException.ThrowIfNull(hostApplication);
        SparkplugNamespace.ValidateNamespaceElement(groupId, nameof(groupId));
        SparkplugNamespace.ValidateNamespaceElement(edgeNodeId, nameof(edgeNodeId));

        var payload = CreateRebirthPayload();
        return hostApplication.PublishEdgeNodeCommandMessageAsync(groupId, edgeNodeId, payload);
    }

    /// <summary>
    ///     Sends a Rebirth command to a specific Device
    /// </summary>
    /// <param name="hostApplication">The Sparkplug Host Application instance</param>
    /// <param name="groupId">The Sparkplug Group ID</param>
    /// <param name="edgeNodeId">The Sparkplug Edge Node ID</param>
    /// <param name="deviceId">The Sparkplug Device ID</param>
    /// <returns>The MQTT Client Publish Result</returns>
    public static Task<MqttClientPublishResult> PublishDeviceRebirthCommandAsync(
        this SparkplugHostApplication hostApplication, string groupId, string edgeNodeId, string deviceId)
    {
        ArgumentNullException.ThrowIfNull(hostApplication);
        SparkplugNamespace.ValidateNamespaceElement(groupId, nameof(groupId));
        SparkplugNamespace.ValidateNamespaceElement(edgeNodeId, nameof(edgeNodeId));
        SparkplugNamespace.ValidateNamespaceElement(deviceId, nameof(deviceId));

        var payload = CreateRebirthPayload(false);
        return hostApplication.PublishDeviceCommandMessageAsync(groupId, edgeNodeId, deviceId, payload);
    }

    /// <summary>
    ///     Creates a payload for Rebirth command
    /// </summary>
    /// <param name="isNodeCommand">Indicates if this is a node command or device command</param>
    /// <returns>Payload object configured for rebirth command</returns>
    private static Payload CreateRebirthPayload(bool isNodeCommand = true)
    {
        return new Payload
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Metrics =
            {
                new Metric
                {
                    Name = isNodeCommand ? "Node Control/Rebirth" : "Device Control/Rebirth",
                    DateType = DataType.Boolean,
                    Value = true
                }
            }
        };
    }
}