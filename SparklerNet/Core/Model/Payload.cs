using JetBrains.Annotations;

namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug B payload is the top-level component that is encoded and used in an MQTT message. It contains some
///     basic information such as a timestamp and a sequence number as well as an array of metrics which contain key/value
///     pairs of data.
/// </summary>
[PublicAPI]
public record Payload
{
    /// <summary>
    ///     The timestamp of the payload in milliseconds since epoch.
    /// </summary>
    public long Timestamp { get; init; }

    /// <summary>
    ///     The array of metrics in the payload.
    /// </summary>
    public List<Metric> Metrics { get; set; } = [];

    /// <summary>
    ///     The sequence number of the payload. 0 ~ 225
    /// </summary>
    public int Seq { get; init; }

    /// <summary>
    ///     The array of bytes which can be used for any custom binary encoded data.
    /// </summary>
    public byte[]? Body { get; init; }
}