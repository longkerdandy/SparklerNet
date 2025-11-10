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
    ///     Whether to enable the message ordering mechanism. The specification requires Sparkplug Host Application to ensure
    ///     that all messages arrive within a Reorder Timeout.
    ///     Since the cache and timeout mechanism actually involves many edge cases to consider, enabling this function will
    ///     incur certain performance overhead and cause delays in out-of-order messages. In specific scenarios, due to the
    ///     Timer reset mechanism, the actual delay may be much longer than the duration set by SeqReorderTimeout. Therefore,
    ///     it is not recommended to enable this function if the device side cannot guarantee that message sequence numbers
    ///     increase sequentially within the range of 0~255.
    ///     The default value is false.
    /// </summary>
    public bool EnableMessageOrdering { get; set; }

    /// <summary>
    ///     The sequence numbers cache expiration time in minutes. This is a special timeout mechanism set for message sequence
    ///     numbers. It is designed to prevent the following messages from being identified as out-of-order messages due to the
    ///     invalidation of cached message sequence numbers after a device has been offline for a long time.
    ///     The default value is 120 minutes.
    /// </summary>
    public int SeqCacheExpiration { get; set; } = 120;

    /// <summary>
    ///     The reorder timeout period in milliseconds for out-of-order messages. This is the Reorder Timer defined in the
    ///     Sparkplug protocol. When an out-of-order message is cached, a Timer will be set for that device. When the Timer
    ///     expires, the messages in the cache will be processed.
    ///     The default value is 10,000 milliseconds (10 seconds).
    /// </summary>
    public int SeqReorderTimeout { get; set; } = 10000;

    /// <summary>
    ///     Whether to send a rebirth command when a reorder timeout occurs. After the Timer expires, the messages in the cache
    ///     will be processed. Subsequently, you can send a Rebirth message to attempt to reset the device's message sequence
    ///     number.
    ///     The default value is true.
    /// </summary>
    public bool SendRebirthWhenTimeout { get; set; } = true;
}