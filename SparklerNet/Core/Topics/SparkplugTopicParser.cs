using System.Text.RegularExpressions;
using SparklerNet.Core.Constants;

namespace SparklerNet.Core.Topics;

public static class SparkplugTopicParser
{
    // Regular expression pattern: matches both regular message and STATE message formats
    // Pattern 1 (Regular messages): <namespace>/<group_id>/<message_type>/<edge_node_id>/[device_id]
    // Pattern 2 (STATE messages): <namespace>/STATE/<host_id>
    private static readonly Regex TopicRegex = new(
        @"^([^/]+)/([^/]+)/([^/]+)/([^/]+)(/([^/]+))?$" + // Pattern 1: Regular messages
        @"|^([^/]+)/(STATE)/([^/]+)$", // Pattern 2: STATE messages
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
    public static (SparkplugVersion version, string? groupId, SparkplugMessageType messageType, string? edgeNodeId,
        string? deviceId, string? hostId)
        ParseTopic(string topic)
    {
        ArgumentNullException.ThrowIfNull(topic);

        var match = TopicRegex.Match(topic);
        if (!match.Success) throw new NotSupportedException($"Not supported Sparkplug topic format: {topic}");

        // Distinguish between regular messages and STATE messages (determined by whether pattern 2 groups match)
        if (match.Groups[7].Success) // Namespace for pattern 2 is in group 7
            // Parse STATE message
            return (
                SparkplugNamespace.ToSparkplugVersion(match.Groups[7].Value),
                null, // STATE messages have no group_id
                SparkplugMessageType.STATE, // Fixed as STATE
                null, // STATE messages have no edge_node_Id
                null, // STATE messages have no device_id
                match.Groups[9].Value
            );

        // Parse regular message
        if (!Enum.TryParse<SparkplugMessageType>(match.Groups[3].Value, true, out var messageType))
            throw new NotSupportedException($"Not supported Sparkplug message type value {match.Groups[3].Value}.");
        return (
            SparkplugNamespace.ToSparkplugVersion(match.Groups[1].Value),
            match.Groups[2].Value,
            messageType,
            match.Groups[4].Value,
            match.Groups[6].Success ? match.Groups[6].Value : null,
            null // Regular messages have no host_id
        );
    }
}