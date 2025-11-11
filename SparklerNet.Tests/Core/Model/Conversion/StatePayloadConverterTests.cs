using System.Buffers;
using System.Text;
using System.Text.Json;
using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class StatePayloadConverterTests
{
    [Theory]
    [InlineData(true, 1620000000L)]
    [InlineData(false, 1620000001L)]
    public void SerializeAndDeserialize_RoundTripPreservesData(bool online, long timestamp)
    {
        var originalPayload = new StatePayload
        {
            Online = online,
            Timestamp = timestamp
        };
        var serialized = StatePayloadConverter.SerializeStatePayload(originalPayload);
        var deserialized = StatePayloadConverter.DeserializeStatePayload(serialized);
        Assert.NotNull(deserialized);
        Assert.Equal(originalPayload.Online, deserialized.Online);
        Assert.Equal(originalPayload.Timestamp, deserialized.Timestamp);
    }

    [Theory]
    [InlineData(true, 1620000000L)]
    [InlineData(false, 1620000001L)]
    public void Deserialize_MultiSegmentSequence_ReturnsCorrectPayload(bool online, long timestamp)
    {
        var originalPayload = new StatePayload
        {
            Online = online,
            Timestamp = timestamp
        };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(originalPayload);
        var sequence = new ReadOnlySequence<byte>(jsonBytes);
        var deserialized = StatePayloadConverter.DeserializeStatePayload(sequence);
        Assert.NotNull(deserialized);
        Assert.Equal(originalPayload.Online, deserialized.Online);
        Assert.Equal(originalPayload.Timestamp, deserialized.Timestamp);
    }

    [Theory]
    [InlineData(null)]
    public void SerializeStatePayload_NullPayload_ThrowsArgumentNullException(StatePayload? payload)
    {
        // Test that null payload throws ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => StatePayloadConverter.SerializeStatePayload(payload!));
    }

    [Theory]
    [InlineData(null)] // Empty sequence
    [InlineData("test")] // Invalid JSON string
    [InlineData("{ \"online\":true,")] // Partial JSON string
    public void DeserializeStatePayload_InvalidInput_ThrowsException(string? input)
    {
        var sequence = input == null
            ? ReadOnlySequence<byte>.Empty
            : new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));

        // All these invalid inputs should throw JsonException
        Assert.Throws<JsonException>(() => StatePayloadConverter.DeserializeStatePayload(sequence));
    }

    [Theory]
    [InlineData("null")]
    public void DeserializeStatePayload_NullJsonString_ThrowsArgumentNullException(string nullJsonString)
    {
        // When a JSON string is exactly "null", JsonSerializer.Deserialize returns null
        var nullJsonBytes = Encoding.UTF8.GetBytes(nullJsonString);
        var sequence = new ReadOnlySequence<byte>(nullJsonBytes);

        // Verify DeserializeStatePayload method throws ArgumentNullException when the result is null
        Assert.Throws<ArgumentNullException>(() => StatePayloadConverter.DeserializeStatePayload(sequence));
    }
}