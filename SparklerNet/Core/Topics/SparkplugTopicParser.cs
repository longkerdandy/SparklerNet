using System.Text.RegularExpressions;
using SparklerNet.Core.Constants;

namespace SparklerNet.Core.Topics;

/// <summary>
///     Provides methods for parsing Sparkplug Topic strings (supports both regular messages and STATE messages).
/// </summary>
public static class SparkplugTopicParser
{
    // Regular expression pattern: matches both regular message and STATE message formats
    // Pattern 1 (Regular messages): <namespace>/<group_id>/<message_type>/<edge_node_id>/[device_id]
    // Pattern 2 (STATE messages): <namespace>/STATE/<host_id>
    private static readonly Regex TopicRegex = new(
        @"^(?<namespace>[^/]+)/(?<groupId>[^/]+)/(?<messageType>[^/]+)/(?<edgeNodeId>[^/]+)(/(?<deviceId>[^/]+))?$" +
        @"|^(?<namespace>[^/]+)/(STATE)/(?<hostId>[^/]+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    ///     Parses a Sparkplug Topic string (supports both regular messages and STATE messages).
    /// </summary>
    /// <param name="topic">Sparkplug Topic string</param>
    /// <returns>
    ///     Tuple: (version, groupId, messageType, edgeNodeId, deviceId, hostId)
    ///     - Regular messages: All fields except hostId are valid (deviceId may be null)
    ///     - STATE messages: messageType is fixed as "STATE", groupId, edgeNodeId and deviceId are null
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the Sparkplug topic format is not supported.</exception>
    public static (SparkplugVersion version, string? groupId, SparkplugMessageType messageType, string? edgeNodeId,
        string? deviceId, string? hostId) ParseTopic(string topic)
    {
        ArgumentNullException.ThrowIfNull(topic);

        var match = TopicRegex.Match(topic);
        if (!match.Success) throw new NotSupportedException($"Not supported Sparkplug topic format: {topic}");

        // Extract the namespace using the named group which works for both patterns
        var namespaceValue = match.Groups["namespace"].Value;
        var version = SparkplugNamespace.ToSparkplugVersion(namespaceValue);

        // Check if it's a STATE message by verifying if the hostId group was matched
        if (match.Groups["hostId"].Success)
            // Parse STATE message
            return (
                version,
                null, // STATE messages have no group_id
                SparkplugMessageType.STATE, // Fixed as STATE
                null, // STATE messages have no edge_node_Id
                null, // STATE messages have no device_id
                match.Groups["hostId"].Value
            );

        // Parse regular message - using named groups for clearer code
        var groupId = match.Groups["groupId"].Value;
        var messageTypeValue = match.Groups["messageType"].Value;
        var edgeNodeId = match.Groups["edgeNodeId"].Value;
        var deviceId = match.Groups["deviceId"].Success ? match.Groups["deviceId"].Value : null;

        if (!Enum.TryParse<SparkplugMessageType>(messageTypeValue, true, out var messageType))
            throw new NotSupportedException($"Not supported Sparkplug message type value {messageTypeValue}.");

        return (
            version,
            groupId,
            messageType,
            edgeNodeId,
            deviceId,
            null // Regular messages have no host_id
        );
    }
}