using System.Buffers;
using System.Text.Json;

namespace SparklerNet.Core.Model.Conversion;

/// <summary>
///     Converts between <see cref="StatePayload" /> and <see cref="ReadOnlySequence{Byte}" />.
/// </summary>
public static class StatePayloadConverter
{
    /// <summary>
    ///     Serializes a <see cref="StatePayload" /> to a <see cref="ReadOnlySequence{Byte}" />.
    /// </summary>
    /// <param name="statePayload">The StatePayload to serialize.</param>
    /// <returns>The serialized <see cref="ReadOnlySequence{Byte}" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="statePayload" /> is null.</exception>
    public static ReadOnlySequence<byte> SerializeStatePayload(StatePayload statePayload)
    {
        ArgumentNullException.ThrowIfNull(statePayload);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(statePayload);
        return new ReadOnlySequence<byte>(bytes);
    }

    /// <summary>
    ///     Deserializes a <see cref="StatePayload" /> from a <see cref="ReadOnlySequence{Byte}" />.
    /// </summary>
    /// <param name="sequence">The sequence of bytes to deserialize.</param>
    /// <returns>The deserialized <see cref="StatePayload" />.</returns>
    /// <exception cref="JsonException">Thrown when deserialization fails.</exception>
    public static StatePayload DeserializeStatePayload(ReadOnlySequence<byte> sequence)
    {
        StatePayload? statePayload;

        if (sequence.IsSingleSegment)
        {
            statePayload = JsonSerializer.Deserialize<StatePayload>(sequence.FirstSpan);
        }
        else
        {
            var buffer = new byte[sequence.Length];
            sequence.CopyTo(buffer);
            statePayload = JsonSerializer.Deserialize<StatePayload>(buffer);
        }

        // Only throw ArgumentNullException if the sequence is "null"
        ArgumentNullException.ThrowIfNull(statePayload, nameof(sequence));

        return statePayload;
    }
}