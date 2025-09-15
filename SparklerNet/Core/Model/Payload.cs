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
    // Milliseconds since epoch.
    public long Timestamp { get; init; }

    // An array of metrics representing key/value/datatype values.
    public List<Metric> Metrics { get; init; } = [];

    // The sequence number. 0 ~ 225
    public int Seq { get; init; }

    // An array of bytes which can be used for any custom binary encoded data.
    public byte[]? Body { get; init; }
}