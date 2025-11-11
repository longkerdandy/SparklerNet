using System.Collections;
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
    public void NullInput_ThrowsArgumentNullException()
    {
        Metric metric = null!;
        ProtoMetric protoMetric = null!;
        Assert.Throws<ArgumentNullException>(() => metric.ToProtoMetric());
        Assert.Throws<ArgumentNullException>(() => protoMetric.ToMetric());
    }

    [Theory]
    [InlineData(DataType.Int32, 42, "int32Metric")]
    [InlineData(DataType.String, "test string", "stringMetric")]
    [InlineData(DataType.DateTime, 1234567890L, "dateTimeMetric")]
    [InlineData(DataType.Boolean, true, "boolMetric")]
    [InlineData(DataType.Float, 3.14f, "floatMetric")]
    [InlineData(DataType.Double, 2.718, "doubleMetric")]
    [InlineData(DataType.Int32, null, "nullIntMetric")]
    public void ToProtoMetric_ReturnsCorrectProtoMetric(DataType dataType, object? value, string metricName)
    {
        var metric = new Metric { Name = metricName, DataType = dataType, Value = value };
        var result = metric.ToProtoMetric();

        Assert.NotNull(result);
        Assert.Equal(metricName, result.Name);
        Assert.Equal((uint)dataType, result.Datatype);
        Assert.Equal(value == null, result.IsNull);

        // Validate the corresponding field based on the data type
        if (value == null) return;
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (dataType)
        {
            case DataType.Int32 when value is int intValue:
                Assert.Equal((uint)intValue, result.IntValue);
                break;
            case DataType.String when value is string stringValue:
                Assert.Equal(stringValue, result.StringValue);
                break;
            case DataType.DateTime when value is long longValue:
                Assert.Equal((ulong)longValue, result.LongValue);
                break;
            case DataType.Boolean when value is bool boolValue:
                // Based on MetricConverter implementation, boolean values use the BooleanValue field
                Assert.Equal(boolValue, result.BooleanValue);
                break;
            case DataType.Float when value is float floatValue:
                Assert.Equal(floatValue, result.FloatValue, 5);
                break;
            case DataType.Double when value is double doubleValue:
                Assert.Equal(doubleValue, result.DoubleValue, 9);
                break;
        }
    }

    [Theory]
    [InlineData(DataType.Int32, 42, "int32Metric")]
    [InlineData(DataType.String, "test string", "stringMetric")]
    [InlineData(DataType.DateTime, 1234567890L, "dateTimeMetric")]
    [InlineData(DataType.Boolean, true, "boolMetric")]
    [InlineData(DataType.Float, 3.14f, "floatMetric")]
    [InlineData(DataType.Double, 2.718, "doubleMetric")]
    [InlineData(DataType.Int32, null, "nullIntMetric")]
    public void ToMetric_ReturnsCorrectMetric(DataType dataType, object? expectedValue, string metricName)
    {
        var protoMetric = new ProtoMetric
        {
            Name = metricName,
            Datatype = (uint)dataType,
            IsNull = expectedValue == null
        };

        // Set the corresponding field based on the data type
        if (expectedValue != null)
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (dataType)
            {
                case DataType.Int32 when expectedValue is int value:
                    protoMetric.IntValue = (uint)value;
                    break;
                case DataType.String when expectedValue is string stringValue:
                    protoMetric.StringValue = stringValue;
                    break;
                case DataType.DateTime when expectedValue is long longValue:
                    protoMetric.LongValue = (ulong)longValue;
                    break;
                case DataType.Boolean when expectedValue is bool boolValue:
                    // From MetricConverter implementation, boolean values use the BooleanValue field
                    protoMetric.BooleanValue = boolValue;
                    break;
                case DataType.Float when expectedValue is float floatValue:
                    protoMetric.FloatValue = floatValue;
                    break;
                case DataType.Double when expectedValue is double doubleValue:
                    protoMetric.DoubleValue = doubleValue;
                    break;
            }

        var result = protoMetric.ToMetric();

        Assert.NotNull(result);
        Assert.Equal(metricName, result.Name);
        Assert.Equal(dataType, result.DataType);

        // Use appropriate comparison method
        switch (expectedValue)
        {
            case float expectedFloat when result.Value is float actualFloat:
                Assert.Equal(expectedFloat, actualFloat, 5);
                break;
            case double expectedDouble when result.Value is double actualDouble:
                Assert.Equal(expectedDouble, actualDouble, 9);
                break;
            default:
                Assert.Equal(expectedValue, result.Value);
                break;
        }
    }

    [Theory]
    [InlineData(999, "unsupportedMetric")]
    public void UnsupportedDataType_ThrowsNotSupportedException(int dataTypeCode, string metricName)
    {
        // Test ToProtoMetric
        var metric = new Metric { Name = metricName, DataType = (DataType)dataTypeCode, Value = "test" };
        Assert.Throws<NotSupportedException>(() => metric.ToProtoMetric());

        // Test ToMetric
        var protoMetric = new ProtoMetric { Name = metricName, Datatype = (uint)dataTypeCode };
        Assert.Throws<NotSupportedException>(() => protoMetric.ToMetric());
    }

    [Theory]
    [ClassData(typeof(NumericArrayTestData))]
    public void MetricRoundTrip_NumericArray_PreservesData(DataType dataType, object arrayValue, string metricName)
    {
        var originalMetric = new Metric { Name = metricName, DataType = dataType, Value = arrayValue };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);

        // Validate array content based on data type
        switch (arrayValue)
        {
            case sbyte[] sbytes:
                Assert.Equal(sbytes, roundTripMetric.Value as sbyte[]);
                break;
            case byte[] bytes:
                Assert.Equal(bytes, roundTripMetric.Value as byte[]);
                break;
            case short[] shorts:
                Assert.Equal(shorts, roundTripMetric.Value as short[]);
                break;
            case ushort[] ushorts:
                Assert.Equal(ushorts, roundTripMetric.Value as ushort[]);
                break;
            case int[] ints:
                Assert.Equal(ints, roundTripMetric.Value as int[]);
                break;
            case uint[] uints:
                Assert.Equal(uints, roundTripMetric.Value as uint[]);
                break;
            case long[] longs:
                Assert.Equal(longs, roundTripMetric.Value as long[]);
                break;
            case ulong[] ulongs:
                Assert.Equal(ulongs, roundTripMetric.Value as ulong[]);
                break;
        }
    }

    [Theory]
    [ClassData(typeof(FloatingPointArrayTestData))]
    public void MetricRoundTrip_FloatingPointArray_PreservesData(DataType dataType, object arrayValue,
        string metricName)
    {
        var originalMetric = new Metric { Name = metricName, DataType = dataType, Value = arrayValue };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);

        // Validate floating point array content using approximate comparison
        switch (arrayValue)
        {
            case float[] floatArray:
            {
                var resultArray = roundTripMetric.Value as float[];
                Assert.NotNull(resultArray);
                Assert.Equal(floatArray.Length, resultArray.Length);
                for (var i = 0; i < floatArray.Length; i++)
                    Assert.Equal(floatArray[i], resultArray[i], 5);
                break;
            }
            case double[] doubleArray:
            {
                var resultArray = roundTripMetric.Value as double[];
                Assert.NotNull(resultArray);
                Assert.Equal(doubleArray.Length, resultArray.Length);
                for (var i = 0; i < doubleArray.Length; i++)
                    Assert.Equal(doubleArray[i], resultArray[i], 9);
                break;
            }
        }
    }

    [Theory]
    [ClassData(typeof(SpecialArrayTestData))]
    public void MetricRoundTrip_SpecialArray_PreservesData(DataType dataType, object arrayValue, string metricName)
    {
        var originalMetric = new Metric { Name = metricName, DataType = dataType, Value = arrayValue };

        var protoMetric = originalMetric.ToProtoMetric();
        var roundTripMetric = protoMetric.ToMetric();

        Assert.NotNull(roundTripMetric);
        Assert.Equal(originalMetric.Name, roundTripMetric.Name);
        Assert.Equal(originalMetric.DataType, roundTripMetric.DataType);

        // Validate special type array content
        switch (arrayValue)
        {
            case bool[] bools:
                Assert.Equal(bools, roundTripMetric.Value as bool[]);
                break;
            case string[] strings:
                Assert.Equal(strings, roundTripMetric.Value as string[]);
                break;
            case long[] longs when dataType == DataType.DateTimeArray:
                Assert.Equal(longs, roundTripMetric.Value as long[]);
                break;
        }
    }

    // Test data classes
    private class NumericArrayTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [DataType.Int8Array, new sbyte[] { -128, 0, 127 }, "int8ArrayMetric"];
            yield return [DataType.UInt8Array, new byte[] { 0, 128, 255 }, "uint8ArrayMetric"];
            yield return [DataType.Int16Array, new short[] { -32768, 0, 32767 }, "int16ArrayMetric"];
            yield return [DataType.UInt16Array, new ushort[] { 0, 32768, 65535 }, "uint16ArrayMetric"];
            yield return [DataType.Int32Array, new[] { -2147483648, 0, 2147483647 }, "int32ArrayMetric"];
            yield return [DataType.UInt32Array, new uint[] { 0, 2147483648, 4294967295 }, "uint32ArrayMetric"];
            yield return
                [DataType.Int64Array, new[] { -9223372036854775808, 0, 9223372036854775807 }, "int64ArrayMetric"];
            yield return
            [
                DataType.UInt64Array, new ulong[] { 0, 9223372036854775808, 18446744073709551615 }, "uint64ArrayMetric"
            ];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class FloatingPointArrayTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [DataType.FloatArray, new[] { -1.5f, 0f, 1.5f }, "floatArrayMetric"];
            yield return [DataType.DoubleArray, new[] { -1.5, 0.0, 1.5 }, "doubleArrayMetric"];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class SpecialArrayTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [DataType.BooleanArray, new[] { true, false, true }, "booleanArrayMetric"];
            yield return [DataType.StringArray, new[] { "test1", "test2", null, "test4" }, "stringArrayMetric"];
            yield return [DataType.DateTimeArray, new[] { 0, 1234567890, 9876543210 }, "dateTimeArrayMetric"];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}