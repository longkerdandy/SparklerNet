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
                    DataType = DataType.Int32,
                    Value = 42,
                    IsHistorical = true,
                    IsTransient = false
                },
                new Metric
                {
                    Name = "testMetric2",
                    Alias = 456,
                    Timestamp = 1620000002L,
                    DataType = DataType.String,
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
            Assert.Equal(originalPayload.Metrics[i].DataType, roundTripPayload.Metrics[i].DataType);
            Assert.Equal(originalPayload.Metrics[i].Value, roundTripPayload.Metrics[i].Value);
            Assert.Equal(originalPayload.Metrics[i].IsHistorical, roundTripPayload.Metrics[i].IsHistorical);
            Assert.Equal(originalPayload.Metrics[i].IsTransient, roundTripPayload.Metrics[i].IsTransient);
        }
    }

    [Theory]
    [InlineData(true)] // Test null Payload to ProtoPayload
    [InlineData(false)] // Test null ProtoPayload to Payload
    public void NullInput_ThrowsArgumentNullException(bool isPayloadTest)
    {
        if (isPayloadTest)
        {
            Payload payload = null!;
            Assert.Throws<ArgumentNullException>(() => payload.ToProtoPayload());
        }
        else
        {
            ProtoPayload protoPayload = null!;
            Assert.Throws<ArgumentNullException>(() => protoPayload.ToPayload());
        }
    }

    [Theory]
    [InlineData(1620000000L, 42, new byte[] { 1, 2, 3, 4, 5 }, false)] // With the normal body
    [InlineData(1620000000L, 42, new byte[0], true)] // With the empty body
    public void PayloadRoundTrip_BodyHandling_PreservesData(long timestamp, int seq, byte[] body,
        bool expectNullBodyAfterRoundTrip)
    {
        var originalPayload = new Payload
        {
            Timestamp = timestamp,
            Seq = seq,
            Body = body,
            Metrics = []
        };

        var protoPayload = originalPayload.ToProtoPayload();
        var roundTripPayload = protoPayload.ToPayload();

        Assert.NotNull(roundTripPayload);
        Assert.Equal(originalPayload.Timestamp, roundTripPayload.Timestamp);
        Assert.Equal(originalPayload.Seq, roundTripPayload.Seq);
        Assert.Empty(roundTripPayload.Metrics);

        if (expectNullBodyAfterRoundTrip)
        {
            Assert.Null(roundTripPayload.Body);
        }
        else
        {
            Assert.NotNull(roundTripPayload.Body);
            Assert.Equal(originalPayload.Body.Length, roundTripPayload.Body.Length);
            for (var i = 0; i < originalPayload.Body.Length; i++)
                Assert.Equal(originalPayload.Body[i], roundTripPayload.Body[i]);
        }
    }

    [Theory]
    [InlineData(1024, 100)] // Large body with a sample verification step
    [InlineData(2048, 200)] // Even larger body with a different verification step
    public void PayloadRoundTrip_LargeBody_PreservesData(int bodySize, int verificationStep)
    {
        // Create a larger byte array
        var largeBody = new byte[bodySize];
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

        // Verify selected bytes to ensure integrity
        for (var i = 0; i < originalPayload.Body.Length; i += verificationStep)
            Assert.Equal(originalPayload.Body[i], roundTripPayload.Body[i]);
    }

    [Theory]
    [InlineData(true)] // Test Payload to ProtoPayload with default values
    [InlineData(false)] // Test ProtoPayload to Payload with default values
    public void DefaultValues_Conversion_PreservesData(bool isPayloadToProtoTest)
    {
        if (isPayloadToProtoTest)
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
            // Check Body handling logic - may return the default value instead of null
            Assert.NotNull(protoPayload.Body);
            Assert.Empty(protoPayload.Body.ToByteArray());
        }
        else
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
    }
}