using SparklerNet.Core.Constants;

namespace SparklerNet.Core.Topics;

/// <summary>
///     The Sparkplug topic factory. Can generate MQTT topic string based on the message type.
/// </summary>
public static class SparkplugTopicFactory
{
    /// <summary>
    ///     The Sparkplug wildcard topic like 'spBv1.0/#' which will match all the Sparkplug related topics.
    /// </summary>
    public static string CreateSparkplugWildcardTopic(SparkplugVersion version)
    {
        return $"{SparkplugNamespace.FromSparkplugVersion(version)}/#";
    }

    /// <summary>
    ///     The Sparkplug Host Application STATE topic MUST be of the form spBv1.0/STATE/sparkplug_host_id where the
    ///     sparkplug_host_id must be replaced with the specific Sparkplug Host ID of this Sparkplug Host Application.
    ///     The topic used for the Host Birth Certificate is identical to the topic used for the Death Certificate.
    /// </summary>
    public static string CreateHostApplicationStateTopic(SparkplugVersion version, string hostId)
    {
        return $"{SparkplugNamespace.FromSparkplugVersion(version)}/STATE/{hostId}";
    }

    /// <summary>
    ///     The NCMD command topic provides the topic namespace used to send commands to any connected Edge Nodes.
    /// </summary>
    public static string CreateEdgeNodeCommandTopic(SparkplugVersion version, string groupId, string edgeNodeId)
    {
        return $"{SparkplugNamespace.FromSparkplugVersion(version)}/{groupId}/NCMD/{edgeNodeId}";
    }

    /// <summary>
    ///     The DCMD command topic provides the topic namespace used to send commands to any connected Devices.
    /// </summary>
    public static string CreateDeviceCommandTopic(SparkplugVersion version, string groupId, string edgeNodeId,
        string deviceId)
    {
        return $"{SparkplugNamespace.FromSparkplugVersion(version)}/{groupId}/DCMD/{edgeNodeId}/{deviceId}";
    }
}