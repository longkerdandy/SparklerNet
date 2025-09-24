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
    /// <param name="version">Sparkplug Version</param>
    /// <param name="hostId">Host Application ID</param>
    /// <returns>Generated Host Application Topic</returns>
    public static string CreateStateTopic(SparkplugVersion version, string hostId)
    {
        return $"{SparkplugNamespace.FromSparkplugVersion(version)}/STATE/{hostId}";
    }

    /// <summary>
    ///     The topic for a Sparkplug Edge Node MUST be of the form namespace/group_id/message_type/edge_node_id
    ///     where the namespace is replaced with the specific namespace for this version of Sparkplug and the group_id and
    ///     edge_node_id are replaced with the Group and Edge Node ID for this specific Edge Node.
    /// </summary>
    /// <param name="version">Sparkplug Version</param>
    /// <param name="groupId">Group ID</param>
    /// <param name="messageType">Sparkplug Message Type</param>
    /// <param name="edgeNodeId">Edge Node ID</param>
    /// <returns>Generated Edge Node Topic</returns>
    public static string CreateEdgeNodeTopic(SparkplugVersion version, string groupId, SparkplugMessageType messageType,
        string edgeNodeId)
    {
        return $"{SparkplugNamespace.FromSparkplugVersion(version)}/{groupId}/{messageType}/{edgeNodeId}";
    }

    /// <summary>
    ///     The topic for a Sparkplug Device MUST be of the form namespace/group_id/message_type/edge_node_id/device_id
    ///     where the namespace is replaced with the specific namespace for this version of Sparkplug and the group_id,
    ///     edge_node_id, and device_id are replaced with the Group, Edge Node, and Device ID for this specific Device.
    /// </summary>
    /// <param name="version">Sparkplug Version</param>
    /// <param name="groupId">Group ID</param>
    /// <param name="messageType">Sparkplug Message Type</param>
    /// <param name="edgeNodeId">Edge Node ID</param>
    /// <param name="deviceId">Device ID</param>
    /// <returns>Generated Device Topic</returns>
    public static string CreateDeviceTopic(SparkplugVersion version, string groupId, SparkplugMessageType messageType,
        string edgeNodeId, string deviceId)
    {
        return
            $"{SparkplugNamespace.FromSparkplugVersion(version)}/{groupId}/{messageType}/{edgeNodeId}/{deviceId}";
    }
}