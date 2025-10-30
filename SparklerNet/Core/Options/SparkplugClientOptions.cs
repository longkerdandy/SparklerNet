using MQTTnet.Packets;
using SparklerNet.Core.Constants;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SparklerNet.Core.Options;

/// <summary>
///     The Sparkplug client options.
/// </summary>
public record SparkplugClientOptions
{
    /// <summary>
    ///     The Sparkplug specification version.
    /// </summary>
    public SparkplugVersion Version { get; set; } = SparkplugVersion.V300;

    /// <summary>
    ///     The Sparkplug Host Application ID.
    ///     Only applies to Host Applications and Primary Host Applications.
    ///     MUST be unique to all other Sparkplug Host IDs in the infrastructure.
    ///     MUST be a valid UTF-8 string except the reserved characters of + (plus), / (forward slash), and # (number sign).
    /// </summary>
    public required string HostApplicationId { get; set; }

    /// <summary>
    ///     List of MQTT topics the Sparkplug client will subscribe to.
    ///     No need to add subscriptions included in the Sparkplug specification. For example, the Host Application does not
    ///     require its own Topic 'spBv1.0/STATE/sparkplug_host_id'.
    /// </summary>
    public List<MqttTopicFilter> Subscriptions { get; set; } = [];
}