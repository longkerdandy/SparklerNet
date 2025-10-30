using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;
using Payload = SparklerNet.Core.Protobuf.Payload;
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
    public void ToProtoParameter_Int8Value_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "int8Param", Type = DataType.Int8, Value = (sbyte)42 };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("int8Param", result.Name);
        Assert.Equal((uint)DataType.Int8, result.Type);
        Assert.Equal((uint)42, result.IntValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.IntValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_Int16Value_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "int16Param", Type = DataType.Int16, Value = (short)42 };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("int16Param", result.Name);
        Assert.Equal((uint)DataType.Int16, result.Type);
        Assert.Equal((uint)42, result.IntValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.IntValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_Int32Value_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "int32Param", Type = DataType.Int32, Value = 42 };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("int32Param", result.Name);
        Assert.Equal((uint)DataType.Int32, result.Type);
        Assert.Equal((uint)42, result.IntValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.IntValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_UInt8Value_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "uint8Param", Type = DataType.UInt8, Value = (byte)42 };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("uint8Param", result.Name);
        Assert.Equal((uint)DataType.UInt8, result.Type);
        Assert.Equal((uint)42, result.IntValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.IntValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_UInt16Value_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "uint16Param", Type = DataType.UInt16, Value = (ushort)42 };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("uint16Param", result.Name);
        Assert.Equal((uint)DataType.UInt16, result.Type);
        Assert.Equal((uint)42, result.IntValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.IntValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_UInt32Value_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "uint32Param", Type = DataType.UInt32, Value = (uint)42 };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("uint32Param", result.Name);
        Assert.Equal((uint)DataType.UInt32, result.Type);
        Assert.Equal((uint)42, result.IntValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.IntValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_Int64Value_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "int64Param", Type = DataType.Int64, Value = (long)42 };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("int64Param", result.Name);
        Assert.Equal((uint)DataType.Int64, result.Type);
        Assert.Equal((ulong)42, result.LongValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.LongValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_UInt64Value_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "uint64Param", Type = DataType.UInt64, Value = (ulong)42 };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("uint64Param", result.Name);
        Assert.Equal((uint)DataType.UInt64, result.Type);
        Assert.Equal((ulong)42, result.LongValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.LongValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_FloatValue_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "floatParam", Type = DataType.Float, Value = 3.14159f };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("floatParam", result.Name);
        Assert.Equal((uint)DataType.Float, result.Type);
        Assert.Equal(3.14159f, result.FloatValue, 5);
        Assert.Equal(ProtoParameter.ValueOneofCase.FloatValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_DoubleValue_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "doubleParam", Type = DataType.Double, Value = 3.14159 };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("doubleParam", result.Name);
        Assert.Equal((uint)DataType.Double, result.Type);
        Assert.Equal(3.14159, result.DoubleValue, 5);
        Assert.Equal(ProtoParameter.ValueOneofCase.DoubleValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_BooleanValue_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "boolParam", Type = DataType.Boolean, Value = true };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("boolParam", result.Name);
        Assert.Equal((uint)DataType.Boolean, result.Type);
        Assert.True(result.BooleanValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.BooleanValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_DateTimeValue_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "dateTimeParam", Type = DataType.DateTime, Value = (long)1234567890 };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("dateTimeParam", result.Name);
        Assert.Equal((uint)DataType.DateTime, result.Type);
        Assert.Equal((ulong)1234567890, result.LongValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.LongValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_StringValue_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "stringParam", Type = DataType.String, Value = "test string" };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("stringParam", result.Name);
        Assert.Equal((uint)DataType.String, result.Type);
        Assert.Equal("test string", result.StringValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.StringValue, result.ValueCase);
    }

    [Fact]
    public void ToProtoParameter_TextValue_ReturnsCorrectProtoParameter()
    {
        var parameter = new Parameter { Name = "textParam", Type = DataType.Text, Value = "test text" };
        var result = parameter.ToProtoParameter();

        Assert.NotNull(result);
        Assert.Equal("textParam", result.Name);
        Assert.Equal((uint)DataType.Text, result.Type);
        Assert.Equal("test text", result.StringValue);
        Assert.Equal(ProtoParameter.ValueOneofCase.StringValue, result.ValueCase);
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

    [Fact]
    public void ToParameter_NullProtoParameter_ThrowsArgumentNullException()
    {
        ProtoParameter protoParameter = null!;
        Assert.Throws<ArgumentNullException>(() => protoParameter.ToParameter());
    }

    [Fact]
    public void ToParameter_Int8ProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "int8Param",
            Type = (uint)DataType.Int8,
            IntValue = 42
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("int8Param", result.Name);
        Assert.Equal(DataType.Int8, result.Type);
        Assert.Equal((sbyte)42, result.Value);
    }

    [Fact]
    public void ToParameter_Int16ProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "int16Param",
            Type = (uint)DataType.Int16,
            IntValue = 42
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("int16Param", result.Name);
        Assert.Equal(DataType.Int16, result.Type);
        Assert.Equal((short)42, result.Value);
    }

    [Fact]
    public void ToParameter_Int32ProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "int32Param",
            Type = (uint)DataType.Int32,
            IntValue = 42
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("int32Param", result.Name);
        Assert.Equal(DataType.Int32, result.Type);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ToParameter_UInt8ProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "uint8Param",
            Type = (uint)DataType.UInt8,
            IntValue = 42
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("uint8Param", result.Name);
        Assert.Equal(DataType.UInt8, result.Type);
        Assert.Equal((byte)42, result.Value);
    }

    [Fact]
    public void ToParameter_UInt16ProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "uint16Param",
            Type = (uint)DataType.UInt16,
            IntValue = 42
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("uint16Param", result.Name);
        Assert.Equal(DataType.UInt16, result.Type);
        Assert.Equal((ushort)42, result.Value);
    }

    [Fact]
    public void ToParameter_UInt32ProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "uint32Param",
            Type = (uint)DataType.UInt32,
            IntValue = 42
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("uint32Param", result.Name);
        Assert.Equal(DataType.UInt32, result.Type);
        Assert.Equal((uint)42, result.Value);
    }

    [Fact]
    public void ToParameter_Int64ProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "int64Param",
            Type = (uint)DataType.Int64,
            LongValue = 42
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("int64Param", result.Name);
        Assert.Equal(DataType.Int64, result.Type);
        Assert.Equal((long)42, result.Value);
    }

    [Fact]
    public void ToParameter_UInt64ProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "uint64Param",
            Type = (uint)DataType.UInt64,
            LongValue = 42
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("uint64Param", result.Name);
        Assert.Equal(DataType.UInt64, result.Type);
        Assert.Equal((ulong)42, result.Value);
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

    [Fact]
    public void ToParameter_DateTimeProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "dateTimeParam",
            Type = (uint)DataType.DateTime,
            LongValue = 1234567890
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("dateTimeParam", result.Name);
        Assert.Equal(DataType.DateTime, result.Type);
        Assert.Equal((long)1234567890, result.Value);
    }

    [Fact]
    public void ToParameter_StringProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "stringParam",
            Type = (uint)DataType.String,
            StringValue = "test string"
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("stringParam", result.Name);
        Assert.Equal(DataType.String, result.Type);
        Assert.Equal("test string", result.Value);
    }

    [Fact]
    public void ToParameter_TextProtoParameter_ReturnsCorrectParameter()
    {
        var protoParameter = new ProtoParameter
        {
            Name = "textParam",
            Type = (uint)DataType.Text,
            StringValue = "test text"
        };

        var result = protoParameter.ToParameter();

        Assert.NotNull(result);
        Assert.Equal("textParam", result.Name);
        Assert.Equal(DataType.Text, result.Type);
        Assert.Equal("test text", result.Value);
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