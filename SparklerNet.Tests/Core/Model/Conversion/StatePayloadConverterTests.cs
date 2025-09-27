using System.Buffers;
using System.Text;
using System.Text.Json;
using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class StatePayloadConverterTests
{
    [Fact]
    public void SerializeAndDeserialize_SingleSegment_RoundTripPreservesData()
    {
        var originalPayload = new StatePayload
        {
            Online = true,
            Timestamp = 1620000000L
        };
        var serialized = StatePayloadConverter.SerializeStatePayload(originalPayload);
        var deserialized = StatePayloadConverter.DeserializeStatePayload(serialized);
        Assert.NotNull(deserialized);
        Assert.Equal(originalPayload.Online, deserialized.Online);
        Assert.Equal(originalPayload.Timestamp, deserialized.Timestamp);
    }

    [Fact]
    public void Deserialize_MultiSegmentSequence_ReturnsCorrectPayload()
    {
        var originalPayload = new StatePayload
        {
            Online = false,
            Timestamp = 1620000001L
        };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(originalPayload);
        var sequence = new ReadOnlySequence<byte>(jsonBytes);
        var deserialized = StatePayloadConverter.DeserializeStatePayload(sequence);
        Assert.NotNull(deserialized);
        Assert.Equal(originalPayload.Online, deserialized.Online);
        Assert.Equal(originalPayload.Timestamp, deserialized.Timestamp);
    }

    [Fact]
    public void SerializeStatePayload_NullPayload_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => StatePayloadConverter.SerializeStatePayload(null!));
    }

    [Fact]
    public void DeserializeStatePayload_EmptySequence_ThrowsException()
    {
        var emptySequence = ReadOnlySequence<byte>.Empty;
        Assert.Throws<JsonException>(() => StatePayloadConverter.DeserializeStatePayload(emptySequence));
    }

    [Fact]
    public void DeserializeStatePayload_InvalidJson_ThrowsException()
    {
        var invalidJsonBytes = "test"u8.ToArray();
        var sequence = new ReadOnlySequence<byte>(invalidJsonBytes);
        Assert.Throws<JsonException>(() => StatePayloadConverter.DeserializeStatePayload(sequence));
    }

    [Fact]
    public void DeserializeStatePayload_PartialJson_ThrowsException()
    {
        var partialJsonBytes = Encoding.UTF8.GetBytes(@"{ ""online"":true,");
        var sequence = new ReadOnlySequence<byte>(partialJsonBytes);
        Assert.Throws<JsonException>(() => StatePayloadConverter.DeserializeStatePayload(sequence));
    }

    [Fact]
    public void DeserializeStatePayload_NullJsonString_ThrowsArgumentNullException()
    {
        // 当JSON字符串恰好是"null"时，JsonSerializer.Deserialize会返回null
        var nullJsonBytes = Encoding.UTF8.GetBytes("null");
        var sequence = new ReadOnlySequence<byte>(nullJsonBytes);

        // 验证DeserializeStatePayload方法在结果为null时抛出ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => StatePayloadConverter.DeserializeStatePayload(sequence));
    }
}