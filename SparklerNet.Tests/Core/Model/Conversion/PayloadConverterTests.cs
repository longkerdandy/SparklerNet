using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;
using ProtoPayload = SparklerNet.Core.Protobuf.Payload;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class PayloadConverterTests
{
    [Fact]
    public void PayloadRoundTrip_PreservesData()
    {
        // Create a Payload with various fields
        var originalPayload = new Payload
        {
            Timestamp = 1620000000L,
            Seq = 42,
            Body = [1, 2, 3, 4, 5],
            Metrics =
            {
                new Metric
                {
                    Name = "testMetric1",
                    Alias = 123,
                    Timestamp = 1620000001L,
                    DateType = DataType.Int32,
                    Value = 42,
                    IsHistorical = true,
                    IsTransient = false
                },
                new Metric
                {
                    Name = "testMetric2",
                    Alias = 456,
                    Timestamp = 1620000002L,
                    DateType = DataType.String,
                    Value = "testValue",
                    IsHistorical = false,
                    IsTransient = true
                }
            }
        };

        // Convert to ProtoPayload and then back to Payload
        var protoPayload = originalPayload.ToProtoPayload();
        var roundTripPayload = protoPayload.ToPayload();

        // Verify that the converted data matches the original data
        Assert.NotNull(roundTripPayload);
        Assert.Equal(originalPayload.Timestamp, roundTripPayload.Timestamp);
        Assert.Equal(originalPayload.Seq, roundTripPayload.Seq);

        // Verify Body property
        Assert.NotNull(originalPayload.Body);
        Assert.NotNull(roundTripPayload.Body);
        Assert.Equal(originalPayload.Body.Length, roundTripPayload.Body.Length);
        for (var i = 0; i < originalPayload.Body.Length; i++)
            Assert.Equal(originalPayload.Body[i], roundTripPayload.Body[i]);
        Assert.Equal(originalPayload.Metrics.Count, roundTripPayload.Metrics.Count);
        for (var i = 0; i < originalPayload.Metrics.Count; i++)
        {
            Assert.Equal(originalPayload.Metrics[i].Name, roundTripPayload.Metrics[i].Name);
            Assert.Equal(originalPayload.Metrics[i].Alias, roundTripPayload.Metrics[i].Alias);
            Assert.Equal(originalPayload.Metrics[i].Timestamp, roundTripPayload.Metrics[i].Timestamp);
            Assert.Equal(originalPayload.Metrics[i].DateType, roundTripPayload.Metrics[i].DateType);
            Assert.Equal(originalPayload.Metrics[i].Value, roundTripPayload.Metrics[i].Value);
            Assert.Equal(originalPayload.Metrics[i].IsHistorical, roundTripPayload.Metrics[i].IsHistorical);
            Assert.Equal(originalPayload.Metrics[i].IsTransient, roundTripPayload.Metrics[i].IsTransient);
        }
    }

    [Fact]
    public void ToProtoPayload_NullPayload_ThrowsArgumentNullException()
    {
        Payload payload = null!;
        Assert.Throws<ArgumentNullException>(() => payload.ToProtoPayload());
    }

    [Fact]
    public void ToPayload_NullProtoPayload_ThrowsArgumentNullException()
    {
        ProtoPayload protoPayload = null!;
        Assert.Throws<ArgumentNullException>(() => protoPayload.ToPayload());
    }

    [Fact]
    public void ToProtoPayload_EmptyPayload_CreatesProtoPayloadWithDefaultValues()
    {
        var payload = new Payload
        {
            Timestamp = 0,
            Seq = 0,
            Metrics = [],
            Body = null
        };

        var protoPayload = payload.ToProtoPayload();

        Assert.NotNull(protoPayload);
        Assert.Equal(0UL, protoPayload.Timestamp);
        Assert.Equal(0UL, protoPayload.Seq);
        Assert.Empty(protoPayload.Metrics);
        // Check Body handling logic - may return default value instead of null
        Assert.NotNull(protoPayload.Body);
        Assert.Empty(protoPayload.Body.ToByteArray());
    }

    [Fact]
    public void ToPayload_EmptyProtoPayload_CreatesPayloadWithDefaultValues()
    {
        var protoPayload = new ProtoPayload
        {
            Timestamp = 0,
            Seq = 0
        };
        // protoPayload.Metrics is already empty by default
        // protoPayload.Body is already null by default

        var payload = protoPayload.ToPayload();

        Assert.NotNull(payload);
        Assert.Equal(0L, payload.Timestamp);
        Assert.Equal(0, payload.Seq);
        Assert.Empty(payload.Metrics);
        Assert.Null(payload.Body);
    }

    [Fact]
    public void PayloadRoundTrip_WithEmptyBody_PreservesData()
    {
        var originalPayload = new Payload
        {
            Timestamp = 1620000000L,
            Seq = 42,
            Body = [],
            Metrics = []
        };

        var protoPayload = originalPayload.ToProtoPayload();
        var roundTripPayload = protoPayload.ToPayload();

        Assert.NotNull(roundTripPayload);
        Assert.Equal(originalPayload.Timestamp, roundTripPayload.Timestamp);
        Assert.Equal(originalPayload.Seq, roundTripPayload.Seq);
        // Check empty array handling - may convert to null based on implementation
        Assert.Null(roundTripPayload.Body);
        Assert.Empty(roundTripPayload.Metrics);
    }

    [Fact]
    public void PayloadRoundTrip_WithLargeBody_PreservesData()
    {
        // Create a larger byte array
        var largeBody = new byte[1024];
        for (var i = 0; i < largeBody.Length; i++) largeBody[i] = (byte)(i % 256);

        var originalPayload = new Payload
        {
            Timestamp = 1620000000L,
            Seq = 42,
            Body = largeBody,
            Metrics = []
        };

        var protoPayload = originalPayload.ToProtoPayload();
        var roundTripPayload = protoPayload.ToPayload();

        Assert.NotNull(roundTripPayload);
        Assert.Equal(originalPayload.Timestamp, roundTripPayload.Timestamp);
        Assert.Equal(originalPayload.Seq, roundTripPayload.Seq);
        Assert.NotNull(roundTripPayload.Body);
        Assert.Equal(originalPayload.Body.Length, roundTripPayload.Body.Length);
        // Verify some bytes to ensure integrity
        for (var i = 0; i < originalPayload.Body.Length; i += 100)
            Assert.Equal(originalPayload.Body[i], roundTripPayload.Body[i]);
    }
}