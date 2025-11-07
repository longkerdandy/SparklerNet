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

    /// <summary>
    ///     Whether to enable message ordering mechanism.
    ///     Default value is false.
    /// </summary>
    public bool EnableMessageOrdering { get; set; } = false;

    /// <summary>
    ///     The sequence number cache expiration time in minutes.
    ///     Default value is 120 minutes.
    /// </summary>
    public int SeqCacheExpiration { get; set; } = 120;

    /// <summary>
    ///     The reorder timeout period in milliseconds for out-of-order messages.
    ///     Default value is 10000 milliseconds (10 seconds).
    /// </summary>
    public int SeqReorderTimeout { get; set; } = 10000;

    /// <summary>
    ///     Whether to process messages that arrived out of sequence if its preceding messages didn’t arrive before the Reorder
    ///     Timeout. Default value is true.
    /// </summary>
    public bool ProcessDisorderedMessages { get; set; } = true;

    /// <summary>
    ///     Whether to send a rebirth command when a reorder timeout occurs. Default value is true.
    /// </summary>
    public bool SendRebirthWhenTimeout { get; set; } = true;
}