using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;
using ProtoParameter = SparklerNet.Core.Protobuf.Payload.Types.Template.Types.Parameter;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class ParameterConverterTests
{
    [Fact]
    public void ParameterRoundTrip_PreservesData()
    {
        var originalParameter = new Parameter
        {
            Name = "roundTripParam",
            Type = DataType.Int32,
            Value = 12345
        };

        var protoParameter = originalParameter.ToProtoParameter();
        var roundTripParameter = protoParameter.ToParameter();

        Assert.NotNull(roundTripParameter);
        Assert.Equal(originalParameter.Name, roundTripParameter.Name);
        Assert.Equal(originalParameter.Type, roundTripParameter.Type);
        Assert.Equal(originalParameter.Value, roundTripParameter.Value);
    }

    [Fact]
    public void ToProtoParameter_NullParameter_ThrowsArgumentNullException()
    {
        Parameter parameter = null!;
        Assert.Throws<ArgumentNullException>(() => parameter.ToProtoParameter());
    }

    [Fact]
    public void ToParameter_NullProtoParameter_ThrowsArgumentNullException()
    {
        ProtoParameter protoParameter = null!;
        Assert.Throws<ArgumentNullException>(() => protoParameter.ToParameter());
    }

    [Theory]
    [InlineData(DataType.Int8, (sbyte)42, "int8Param", 42UL, ProtoParameter.ValueOneofCase.IntValue)]
    [InlineData(DataType.Int16, (short)42, "int16Param", 42UL, ProtoParameter.ValueOneofCase.IntValue)]
    [InlineData(DataType.Int32, 42, "int32Param", 42UL, ProtoParameter.ValueOneofCase.IntValue)]
    [InlineData(DataType.UInt8, (byte)42, "uint8Param", 42UL, ProtoParameter.ValueOneofCase.IntValue)]
    [InlineData(DataType.UInt16, (ushort)42, "uint16Param", 42UL, ProtoParameter.ValueOneofCase.IntValue)]
    [InlineData(DataType.UInt32, (uint)42, "uint32Param", 42UL, ProtoParameter.ValueOneofCase.IntValue)]
    public void ToProtoParameter_IntValueType_ReturnsCorrectProtoParameter(
        DataType dataType,
        object value,
        string parameterName,
        ulong expectedIntValue,
        ProtoParameter.ValueOneofCase expectedValueCase)
    {
        var parameter = new Parameter { Name = parameterName, Type = dataType, Value = value };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal(parameterName, result.Name);
        Assert.Equal((uint)dataType, result.Type);
        Assert.Equal(expectedIntValue, result.IntValue);
        Assert.Equal(expectedValueCase, result.ValueCase);
    }

    [Theory]
    [InlineData(DataType.Int64, (long)42, "int64Param", 42UL, ProtoParameter.ValueOneofCase.LongValue)]
    [InlineData(DataType.UInt64, (ulong)42, "uint64Param", 42UL, ProtoParameter.ValueOneofCase.LongValue)]
    [InlineData(DataType.DateTime, (long)1234567890, "dateTimeParam", 1234567890UL,
        ProtoParameter.ValueOneofCase.LongValue)]
    public void ToProtoParameter_LongValueType_ReturnsCorrectProtoParameter(
        DataType dataType,
        object value,
        string parameterName,
        ulong expectedLongValue,
        ProtoParameter.ValueOneofCase expectedValueCase)
    {
        var parameter = new Parameter { Name = parameterName, Type = dataType, Value = value };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal(parameterName, result.Name);
        Assert.Equal((uint)dataType, result.Type);
        Assert.Equal(expectedLongValue, result.LongValue);
        Assert.Equal(expectedValueCase, result.ValueCase);
    }

    [Theory]
    [InlineData(DataType.Float, 3.14159f, "floatParam", 3.14159f, ProtoParameter.ValueOneofCase.FloatValue)]
    public void ToProtoParameter_FloatValueType_ReturnsCorrectProtoParameter(
        DataType dataType,
        object value,
        string parameterName,
        float expectedFloatValue,
        ProtoParameter.ValueOneofCase expectedValueCase)
    {
        var parameter = new Parameter { Name = parameterName, Type = dataType, Value = value };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal(parameterName, result.Name);
        Assert.Equal((uint)dataType, result.Type);
        Assert.Equal(expectedFloatValue, result.FloatValue, 5);
        Assert.Equal(expectedValueCase, result.ValueCase);
    }

    [Theory]
    [InlineData(DataType.Double, 3.14159, "doubleParam", 3.14159, ProtoParameter.ValueOneofCase.DoubleValue)]
    public void ToProtoParameter_DoubleValueType_ReturnsCorrectProtoParameter(
        DataType dataType,
        object value,
        string parameterName,
        double expectedDoubleValue,
        ProtoParameter.ValueOneofCase expectedValueCase)
    {
        var parameter = new Parameter { Name = parameterName, Type = dataType, Value = value };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal(parameterName, result.Name);
        Assert.Equal((uint)dataType, result.Type);
        Assert.Equal(expectedDoubleValue, result.DoubleValue, 5);
        Assert.Equal(expectedValueCase, result.ValueCase);
    }

    [Theory]
    [InlineData(DataType.Boolean, true, "boolParam", ProtoParameter.ValueOneofCase.BooleanValue)]
    public void ToProtoParameter_BooleanValueType_ReturnsCorrectProtoParameter(
        DataType dataType,
        object value,
        string parameterName,
        ProtoParameter.ValueOneofCase expectedValueCase)
    {
        var parameter = new Parameter { Name = parameterName, Type = dataType, Value = value };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal(parameterName, result.Name);
        Assert.Equal((uint)dataType, result.Type);
        Assert.True(result.BooleanValue);
        Assert.Equal(expectedValueCase, result.ValueCase);
    }

    [Theory]
    [InlineData(DataType.String, "test string", "stringParam", "test string",
        ProtoParameter.ValueOneofCase.StringValue)]
    [InlineData(DataType.Text, "test text", "textParam", "test text", ProtoParameter.ValueOneofCase.StringValue)]
    public void ToProtoParameter_StringType_ReturnsCorrectProtoParameter(
        DataType dataType,
        object value,
        string parameterName,
        string expectedStringValue,
        ProtoParameter.ValueOneofCase expectedValueCase)
    {
        var parameter = new Parameter { Name = parameterName, Type = dataType, Value = value };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal(parameterName, result.Name);
        Assert.Equal((uint)dataType, result.Type);
        Assert.Equal(expectedStringValue, result.StringValue);
        Assert.Equal(expectedValueCase, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_NullValue_ReturnsParameterWithoutValue()
    {
        var parameter = new Parameter { Name = "nullParam", Type = DataType.String, Value = null };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("nullParam", result.Name);
        Assert.Equal((uint)DataType.String, result.Type);
        Assert.Equal(ProtoParameter.ValueOneofCase.None, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_UnsupportedDataType_ThrowsNotSupportedException()
    {
        var parameter = new Parameter { Name = "unsupportedParam", Type = DataType.UUID, Value = new byte[16] };
        Assert.Throws<NotSupportedException>(() => parameter.ToProtoParameter());
    }

    [Theory]
    [InlineData(DataType.Int8, "int8Param", (sbyte)42)]
    [InlineData(DataType.Int16, "int16Param", (short)42)]
    [InlineData(DataType.Int32, "int32Param", 42)]
    [InlineData(DataType.UInt8, "uint8Param", (byte)42)]
    [InlineData(DataType.UInt16, "uint16Param", (ushort)42)]
    [InlineData(DataType.UInt32, "uint32Param", (uint)42)]
    public void ToParameter_IntValueType_ReturnsCorrectParameter(
        DataType dataType,
        string parameterName,
        object expectedValue)
    {
        var protoParameter = new ProtoParameter
        {
            Name = parameterName,
            Type = (uint)dataType,
            IntValue = 42
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal(parameterName, result.Name);
        Assert.Equal(dataType, result.Type);
        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [InlineData(DataType.Int64, "int64Param", (long)42)]
    [InlineData(DataType.UInt64, "uint64Param", (ulong)42)]
    [InlineData(DataType.DateTime, "dateTimeParam", (long)1234567890)]
    public void ToParameter_LongValueType_ReturnsCorrectParameter(
        DataType dataType,
        string parameterName,
        object expectedValue)
    {
        var protoParameter = new ProtoParameter
        {
            Name = parameterName,
            Type = (uint)dataType,
            LongValue = dataType == DataType.DateTime ? 1234567890UL : 42UL
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal(parameterName, result.Name);
        Assert.Equal(dataType, result.Type);
        Assert.Equal(expectedValue, result.Value);
    }

    [Fact]
    public void ToParameter_FloatProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "floatParam",
            Type = (uint)DataType.Float,
            FloatValue = 3.14159f
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("floatParam", result.Name);
        Assert.Equal(DataType.Float, result.Type);
        Assert.Equal(3.14159f, Convert.ToSingle(result.Value), 5);
    }

    [Fact]
    public void ToParameter_DoubleProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "doubleParam",
            Type = (uint)DataType.Double,
            DoubleValue = 3.14159
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("doubleParam", result.Name);
        Assert.Equal(DataType.Double, result.Type);
        Assert.Equal(3.14159, Convert.ToDouble(result.Value), 5);
    }

    [Fact]
    public void ToParameter_BooleanProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "boolParam",
            Type = (uint)DataType.Boolean,
            BooleanValue = true
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("boolParam", result.Name);
        Assert.Equal(DataType.Boolean, result.Type);
        Assert.Equal(true, result.Value);
    }

    [Theory]
    [InlineData(DataType.String, "stringParam", "test string")]
    [InlineData(DataType.Text, "textParam", "test text")]
    public void ToParameter_StringType_ReturnsCorrectParameter(
        DataType dataType,
        string parameterName,
        string expectedValue)
    {
        var protoParameter = new ProtoParameter
        {
            Name = parameterName,
            Type = (uint)dataType,
            StringValue = expectedValue
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal(parameterName, result.Name);
        Assert.Equal(dataType, result.Type);
        Assert.Equal(expectedValue, result.Value);
    }

    [Fact]
    public void ToParameter_ProtoParameterWithoutValue_ReturnsParameterWithNullValue()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "nullParam",
            Type = (uint)DataType.String
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("nullParam", result.Name);
        Assert.Equal(DataType.String, result.Type);
        Assert.Null(result.Value);
    }

    [Fact]
    public void ToParameter_UnsupportedDataType_ThrowsNotSupportedException()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "unsupportedParam",
            Type = (uint)DataType.UUID,
            StringValue = "a81bc81b-dead-4e5d-abff-90865d1e13b1"
        };

        Assert.Throws<NotSupportedException>(() => protoParameter.ToParameter());
    }
}