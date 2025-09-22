using JetBrains.Annotations;
using static Google.Protobuf.ByteString;
using ProtoPayload = SparklerNet.Core.Protobuf.Payload;

namespace SparklerNet.Core.Model.Conversion;

/// <summary>
///     Converts between <see cref="Payload" /> and <see cref="ProtoPayload" />.
/// </summary>
[PublicAPI]
public static class PayloadConverter
{
    /// <summary>
    ///     Converts a <see cref="Payload" /> to a Protobuf <see cref="ProtoPayload" />.
    /// </summary>
    /// <param name="payload">The Payload to convert.</param>
    /// <returns>The converted Protobuf Payload.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload" /> is null.</exception>
    public static ProtoPayload ToProtoPayload(this Payload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var protoPayload = new ProtoPayload
        {
            // Set basic properties
            Timestamp = (ulong)payload.Timestamp,
            Seq = (ulong)payload.Seq
        };

        // Convert and add metrics
        foreach (var metric in payload.Metrics) protoPayload.Metrics.Add(metric.ToProtoMetric());

        // Set body if it has a value
        if (payload.Body != null) protoPayload.Body = CopyFrom(payload.Body);

        return protoPayload;
    }

    /// <summary>
    ///     Converts a Protobuf <see cref="ProtoPayload" /> to a <see cref="Payload" />.
    /// </summary>
    /// <param name="protoPayload">The Protobuf Payload to convert.</param>
    /// <returns>The converted Payload.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protoPayload" /> is null.</exception>
    public static Payload ToPayload(this ProtoPayload protoPayload)
    {
        ArgumentNullException.ThrowIfNull(protoPayload);

        // Get body bytes if available
        byte[]? bodyBytes = null;
        if (protoPayload.Body != null && protoPayload.Body.Length > 0) bodyBytes = protoPayload.Body.ToByteArray();

        // Create payload with all init properties in initializer
        var payload = new Payload
        {
            // Set basic properties
            Timestamp = (long)protoPayload.Timestamp,
            Seq = (int)protoPayload.Seq,
            Body = bodyBytes
        };

        // Convert and add metrics
        foreach (var metric in protoPayload.Metrics) payload.Metrics.Add(metric.ToMetric());

        return payload;
    }
}