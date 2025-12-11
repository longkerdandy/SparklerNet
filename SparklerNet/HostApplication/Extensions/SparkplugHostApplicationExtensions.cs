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
    ///     Sends a Scan Rate command to a specific Edge Node
    /// </summary>
    /// <param name="hostApplication">The Sparkplug Host Application instance</param>
    /// <param name="groupId">The Sparkplug Group ID</param>
    /// <param name="edgeNodeId">The Sparkplug Edge Node ID</param>
    /// <param name="scanRate">The scan rate value in milliseconds</param>
    /// <returns>The MQTT Client Publish Result</returns>
    public static Task<MqttClientPublishResult> PublishEdgeNodeScanRateCommandAsync(
        this SparkplugHostApplication hostApplication, string groupId, string edgeNodeId, long scanRate)
    {
        ArgumentNullException.ThrowIfNull(hostApplication);
        SparkplugNamespace.ValidateNamespaceElement(groupId, nameof(groupId));
        SparkplugNamespace.ValidateNamespaceElement(edgeNodeId, nameof(edgeNodeId));
        if (scanRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(scanRate), scanRate, "Scan rate must be greater than zero.");

        var payload = CreateScanRatePayload(true, scanRate);
        return hostApplication.PublishEdgeNodeCommandMessageAsync(groupId, edgeNodeId, payload);
    }

    /// <summary>
    ///     Sends a Scan Rate command to a specific Device
    /// </summary>
    /// <param name="hostApplication">The Sparkplug Host Application instance</param>
    /// <param name="groupId">The Sparkplug Group ID</param>
    /// <param name="edgeNodeId">The Sparkplug Edge Node ID</param>
    /// <param name="deviceId">The Sparkplug Device ID</param>
    /// <param name="scanRate">The scan rate value in milliseconds</param>
    /// <returns>The MQTT Client Publish Result</returns>
    public static Task<MqttClientPublishResult> PublishDeviceScanRateCommandAsync(
        this SparkplugHostApplication hostApplication, string groupId, string edgeNodeId, string deviceId,
        long scanRate)
    {
        ArgumentNullException.ThrowIfNull(hostApplication);
        SparkplugNamespace.ValidateNamespaceElement(groupId, nameof(groupId));
        SparkplugNamespace.ValidateNamespaceElement(edgeNodeId, nameof(edgeNodeId));
        SparkplugNamespace.ValidateNamespaceElement(deviceId, nameof(deviceId));
        if (scanRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(scanRate), scanRate, "Scan rate must be greater than zero.");

        var payload = CreateScanRatePayload(false, scanRate);
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
                    DataType = DataType.Boolean,
                    Value = true
                }
            }
        };
    }

    /// <summary>
    ///     Creates a payload for Scan Rate command
    /// </summary>
    /// <param name="isNodeCommand">Indicates if this is a node command or device command</param>
    /// <param name="value">The scan rate value in milliseconds</param>
    /// <returns>Payload object configured for scan rate command</returns>
    private static Payload CreateScanRatePayload(bool isNodeCommand, long value)
    {
        return new Payload
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Metrics =
            {
                new Metric
                {
                    Name = isNodeCommand ? "Node Control/Scan Rate" : "Device Control/Scan Rate",
                    DataType = DataType.Int64,
                    Value = value
                }
            }
        };
    }
}