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
            DataType = DataType.Int32,
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
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
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
        var metric = new Metric { Name = "int32Metric", DataType = DataType.Int32, Value = 42 };
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
        Assert.Equal(DataType.Int32, result.DataType);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ToProtoMetric_StringValue_ReturnsCorrectProtoMetric()
    {
        var metric = new Metric { Name = "stringMetric", DataType = DataType.String, Value = "test string" };
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
        Assert.Equal(DataType.String, result.DataType);
        Assert.Equal("test string", result.Value);
    }

    [Fact]
    public void ToProtoMetric_DateTimeValue_ReturnsCorrectProtoMetric()
    {
        var metric = new Metric { Name = "dateTimeMetric", DataType = DataType.DateTime, Value = 1234567890L };
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
        Assert.Equal(DataType.DateTime, result.DataType);
        Assert.Equal(1234567890L, result.Value);
    }

    [Fact]
    public void ToProtoMetric_NullValue_ReturnsProtoMetricWithIsNullTrue()
    {
        var metric = new Metric { Name = "nullMetric", DataType = DataType.Int32, Value = null };
        var result = metric.ToProtoMetric();

        Assert.NotNull(result);
        Assert.Equal("nullMetric", result.Name);
        Assert.Equal((uint)DataType.Int32, result.Datatype);
        Assert.True(result.IsNull);
        Assert.Equal(0U, result.IntValue);
    }

    [Fact]
    public void ToProtoMetric_UnsupportedDataType_ThrowsNotSupportedException()
    {
        var metric = new Metric { Name = "unsupportedMetric", DataType = (DataType)999, Value = "test" };
        Assert.Throws<NotSupportedException>(() => metric.ToProtoMetric());
    }

    [Fact]
    public void ToMetric_UnsupportedDataType_ThrowsNotSupportedException()
    {
        var protoMetric = new ProtoMetric { Name = "unsupportedMetric", Datatype = 999 };
        Assert.Throws<NotSupportedException>(() => protoMetric.ToMetric());
    }

    [Fact]
    public void MetricRoundTrip_Int8Array_PreservesData()
    {
        var originalArray = new sbyte[] { -128, 0, 127 };
        var originalMetric = new Metric
            { Name = "int8ArrayMetric", DataType = DataType.Int8Array, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as sbyte[]);
    }

    [Fact]
    public void MetricRoundTrip_UInt8Array_PreservesData()
    {
        var originalArray = new byte[] { 0, 128, 255 };
        var originalMetric = new Metric
            { Name = "uint8ArrayMetric", DataType = DataType.UInt8Array, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as byte[]);
    }

    [Fact]
    public void MetricRoundTrip_Int16Array_PreservesData()
    {
        var originalArray = new short[] { -32768, 0, 32767 };
        var originalMetric = new Metric
            { Name = "int16ArrayMetric", DataType = DataType.Int16Array, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as short[]);
    }

    [Fact]
    public void MetricRoundTrip_UInt16Array_PreservesData()
    {
        var originalArray = new ushort[] { 0, 32768, 65535 };
        var originalMetric = new Metric
            { Name = "uint16ArrayMetric", DataType = DataType.UInt16Array, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as ushort[]);
    }

    [Fact]
    public void MetricRoundTrip_Int32Array_PreservesData()
    {
        var originalArray = new[] { -2147483648, 0, 2147483647 };
        var originalMetric = new Metric
            { Name = "int32ArrayMetric", DataType = DataType.Int32Array, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as int[]);
    }

    [Fact]
    public void MetricRoundTrip_UInt32Array_PreservesData()
    {
        var originalArray = new uint[] { 0, 2147483648, 4294967295 };
        var originalMetric = new Metric
            { Name = "uint32ArrayMetric", DataType = DataType.UInt32Array, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as uint[]);
    }

    [Fact]
    public void MetricRoundTrip_Int64Array_PreservesData()
    {
        var originalArray = new[] { -9223372036854775808, 0, 9223372036854775807 };
        var originalMetric = new Metric
            { Name = "int64ArrayMetric", DataType = DataType.Int64Array, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as long[]);
    }

    [Fact]
    public void MetricRoundTrip_UInt64Array_PreservesData()
    {
        var originalArray = new ulong[] { 0, 9223372036854775808, 18446744073709551615 };
        var originalMetric = new Metric
            { Name = "uint64ArrayMetric", DataType = DataType.UInt64Array, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as ulong[]);
    }

    [Fact]
    public void MetricRoundTrip_FloatArray_PreservesData()
    {
        var originalArray = new[] { -1.5f, 0f, 1.5f };
        var originalMetric = new Metric
            { Name = "floatArrayMetric", DataType = DataType.FloatArray, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as float[]);
    }

    [Fact]
    public void MetricRoundTrip_DoubleArray_PreservesData()
    {
        var originalArray = new[] { -1.5, 0.0, 1.5 };
        var originalMetric = new Metric
            { Name = "doubleArrayMetric", DataType = DataType.DoubleArray, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as double[]);
    }

    [Fact]
    public void MetricRoundTrip_BooleanArray_PreservesData()
    {
        var originalArray = new[] { true, false, true };
        var originalMetric = new Metric
            { Name = "booleanArrayMetric", DataType = DataType.BooleanArray, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as bool[]);
    }

    [Fact]
    public void MetricRoundTrip_StringArray_PreservesData()
    {
        var originalArray = new[] { "test1", "test2", null, "test4" };
        var originalMetric = new Metric
            { Name = "stringArrayMetric", DataType = DataType.StringArray, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as string[]);
    }

    [Fact]
    public void MetricRoundTrip_DateTimeArray_PreservesData()
    {
        var originalArray = new[] { 0, 1234567890, 9876543210 };
        var originalMetric = new Metric
            { Name = "dateTimeArrayMetric", DataType = DataType.DateTimeArray, Value = originalArray };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);
        Assert.Equal(originalArray, roundTripMetric.Value as long[]);
    }
}