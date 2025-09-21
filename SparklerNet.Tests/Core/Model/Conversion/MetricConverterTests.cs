using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;
using ProtoMetric = SparklerNet.Core.Protobuf.Payload.Types.Metric;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class MetricConverterTests
{
    [Fact]
    public void MetricRoundTrip_PreservesData()
    {
        var originalMetric = new Metric
        {
            Name = "testMetric",
            Alias = 123,
            Timestamp = 1620000000L,
            DateType = DataType.Int32,
            Value = 42,
            IsHistorical = true,
            IsTransient = false
        };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.Alias, roundTripMetric.Alias);
        Assert.Equal(originalMetric.Timestamp, roundTripMetric.Timestamp);
        Assert.Equal(originalMetric.DateType, roundTripMetric.DateType);
        Assert.Equal(originalMetric.Value, roundTripMetric.Value);
        Assert.Equal(originalMetric.IsHistorical, roundTripMetric.IsHistorical);
        Assert.Equal(originalMetric.IsTransient, roundTripMetric.IsTransient);
        Assert.Equal(originalMetric.IsNull, roundTripMetric.IsNull);
    }

    [Fact]
    public void ToProtoMetric_NullMetric_ThrowsArgumentNullException()
    {
        Metric metric = null!;
        Assert.Throws<ArgumentNullException>(() => metric.ToProtoMetric());
    }

    [Fact]
    public void ToMetric_NullProtoMetric_ThrowsArgumentNullException()
    {
        ProtoMetric protoMetric = null!;
        Assert.Throws<ArgumentNullException>(() => protoMetric.ToMetric());
    }

    [Fact]
    public void ToProtoMetric_Int32Value_ReturnsCorrectProtoMetric()
    {
        var metric = new Metric { Name = "int32Metric", DateType = DataType.Int32, Value = 42 };
        var result = metric.ToProtoMetric();

        Assert.NotNull(result);
        Assert.Equal("int32Metric", result.Name);
        Assert.Equal((uint)DataType.Int32, result.Datatype);
        Assert.Equal(42U, result.IntValue);
    }

    [Fact]
    public void ToMetric_Int32Value_ReturnsCorrectMetric()
    {
        var protoMetric = new ProtoMetric { Name = "int32Metric", Datatype = (uint)DataType.Int32, IntValue = 42 };
        var result = protoMetric.ToMetric();

        Assert.NotNull(result);
        Assert.Equal("int32Metric", result.Name);
        Assert.Equal(DataType.Int32, result.DateType);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ToProtoMetric_StringValue_ReturnsCorrectProtoMetric()
    {
        var metric = new Metric { Name = "stringMetric", DateType = DataType.String, Value = "test string" };
        var result = metric.ToProtoMetric();

        Assert.NotNull(result);
        Assert.Equal("stringMetric", result.Name);
        Assert.Equal((uint)DataType.String, result.Datatype);
        Assert.Equal("test string", result.StringValue);
    }

    [Fact]
    public void ToMetric_StringValue_ReturnsCorrectMetric()
    {
        var protoMetric = new ProtoMetric
            { Name = "stringMetric", Datatype = (uint)DataType.String, StringValue = "test string" };
        var result = protoMetric.ToMetric();

        Assert.NotNull(result);
        Assert.Equal("stringMetric", result.Name);
        Assert.Equal(DataType.String, result.DateType);
        Assert.Equal("test string", result.Value);
    }

    [Fact]
    public void ToProtoMetric_DateTimeValue_ReturnsCorrectProtoMetric()
    {
        var metric = new Metric { Name = "dateTimeMetric", DateType = DataType.DateTime, Value = 1234567890L };
        var result = metric.ToProtoMetric();

        Assert.NotNull(result);
        Assert.Equal("dateTimeMetric", result.Name);
        Assert.Equal((uint)DataType.DateTime, result.Datatype);
        Assert.Equal(1234567890UL, result.LongValue);
    }

    [Fact]
    public void ToMetric_DateTimeValue_ReturnsCorrectMetric()
    {
        var protoMetric = new ProtoMetric
            { Name = "dateTimeMetric", Datatype = (uint)DataType.DateTime, LongValue = 1234567890 };
        var result = protoMetric.ToMetric();

        Assert.NotNull(result);
        Assert.Equal("dateTimeMetric", result.Name);
        Assert.Equal(DataType.DateTime, result.DateType);
        Assert.Equal(1234567890L, result.Value);
    }

    [Fact]
    public void ToProtoMetric_NullValue_ReturnsProtoMetricWithIsNullTrue()
    {
        var metric = new Metric { Name = "nullMetric", DateType = DataType.Int32, Value = null };
        var result = metric.ToProtoMetric();

        Assert.NotNull(result);
        Assert.Equal("nullMetric", result.Name);
        Assert.Equal((uint)DataType.Int32, result.Datatype);
        Assert.True(result.IsNull);
        Assert.Equal(0U, result.IntValue); // Default value for IntValue when IsNull is true
    }

    [Fact]
    public void ToProtoMetric_UnsupportedDataType_ThrowsNotSupportedException()
    {
        var metric = new Metric { Name = "unsupportedMetric", DateType = (DataType)999, Value = "test" };
        Assert.Throws<NotSupportedException>(() => metric.ToProtoMetric());
    }

    [Fact]
    public void ToMetric_UnsupportedDataType_ThrowsNotSupportedException()
    {
        var protoMetric = new ProtoMetric { Name = "unsupportedMetric", Datatype = 999 };
        Assert.Throws<NotSupportedException>(() => protoMetric.ToMetric());
    }
}