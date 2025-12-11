// ReSharper disable PropertyCanBeMadeInitOnly.Global

using System.Diagnostics.CodeAnalysis;

namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug B payload is the top-level component that is encoded and used in an MQTT message. It contains some
///     basic information such as a timestamp and a sequence number as well as an array of metrics which contain key/value
///     pairs of data.
/// </summary>
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

    /// <summary>
    ///     Gets the bdSeq (Birth/Death Sequence) metric value from the payload.
    ///     bdSeq is a special metric used in Birth and Death messages to ensure proper sequence tracking.
    /// </summary>
    /// <returns>The bdSeq value if found and datatype is supported, otherwise 0</returns>
    [SuppressMessage("ReSharper", "InvertIf")]
    public int GetBdSeq()
    {
        // Find the bdSeq metric in the metrics list
        var bdSeqMetric = Metrics.FirstOrDefault(m => m.Name == "bdSeq");

        // Check if the metric exists and has a value
        if (bdSeqMetric is { Value: not null, DataType: not null })
        {
            // List of supported data types that can be converted to int
            var supportedTypes = new[]
            {
                DataType.Int16, DataType.Int32, DataType.Int64, DataType.UInt8,
                DataType.UInt16, DataType.UInt32, DataType.UInt64
            };

            // Check if the data type is supported
            if (supportedTypes.Contains(bdSeqMetric.DataType.Value))
                try
                {
                    return Convert.ToInt32(bdSeqMetric.Value);
                }
                catch (OverflowException)
                {
                    return 0;
                }
        }

        // Return default value 0 if the metric is not found, has no value
        return 0;
    }
}