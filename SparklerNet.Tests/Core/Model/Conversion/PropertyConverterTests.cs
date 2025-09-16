using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;
using ProtoPropertyValue = SparklerNet.Core.Protobuf.Payload.Types.PropertyValue;
using ProtoPropertySet = SparklerNet.Core.Protobuf.Payload.Types.PropertySet;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class PropertyConverterTests
{
    [Fact]
    public void ToProtoPropertyValue_NullPropertyValue_ThrowsArgumentNullException()
    {
        PropertyValue property = null!;
        Assert.Throws<ArgumentNullException>(() => property.ToProtoPropertyValue());
    }

    [Fact]
    public void ToProtoPropertyValue_Int32Value_ReturnsCorrectProtoProperty()
    {
        var property = new PropertyValue { Type = DataType.Int32, Value = 42 };
        var result = property.ToProtoPropertyValue();
        Assert.NotNull(result);
        Assert.Equal((uint)DataType.Int32, result.Type);
        Assert.Equal((uint)42, result.IntValue);
    }

    [Fact]
    public void ToProtoPropertyValue_StringValue_ReturnsCorrectProtoProperty()
    {
        var property = new PropertyValue { Type = DataType.String, Value = "test string" };
        var result = property.ToProtoPropertyValue();
        Assert.NotNull(result);
        Assert.Equal((uint)DataType.String, result.Type);
        Assert.Equal("test string", result.StringValue);
    }

    [Fact]
    public void ToProtoPropertyValue_DoubleValue_ReturnsCorrectProtoProperty()
    {
        var property = new PropertyValue { Type = DataType.Double, Value = 3.14159 };
        var result = property.ToProtoPropertyValue();
        Assert.NotNull(result);
        Assert.Equal((uint)DataType.Double, result.Type);
        Assert.Equal(3.14159, result.DoubleValue, 5);
    }

    [Fact]
    public void ToProtoPropertyValue_NullValue_ReturnsProtoPropertyWithNullFlag()
    {
        var property = new PropertyValue { Type = DataType.String, Value = null };
        var result = property.ToProtoPropertyValue();
        Assert.NotNull(result);
        Assert.Equal((uint)DataType.String, result.Type);
        Assert.True(result.IsNull);
    }

    [Fact]
    public void ToProtoPropertyValue_BooleanValue_ReturnsCorrectProtoProperty()
    {
        var property = new PropertyValue { Type = DataType.Boolean, Value = true };
        var result = property.ToProtoPropertyValue();
        Assert.NotNull(result);
        Assert.Equal((uint)DataType.Boolean, result.Type);
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void ToProtoPropertyValue_DateTimeValue_ReturnsCorrectProtoProperty()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var property = new PropertyValue { Type = DataType.DateTime, Value = timestamp };
        var result = property.ToProtoPropertyValue();
        Assert.NotNull(result);
        Assert.Equal((uint)DataType.DateTime, result.Type);
        Assert.Equal((ulong)timestamp, result.LongValue);
    }

    [Fact]
    public void ToProtoPropertyValue_EnumValueNotSupported_ThrowsNotSupportedException()
    {
        var property = new PropertyValue { Type = DataType.UUID, Value = new byte[16] };
        Assert.Throws<NotSupportedException>(() => property.ToProtoPropertyValue());
    }

    [Fact]
    public void ToProtoPropertySet_NullPropertySet_ThrowsArgumentNullException()
    {
        PropertySet propertySet = null!;
        Assert.Throws<ArgumentNullException>(() => propertySet.ToProtoPropertySet());
    }

    [Fact]
    public void ToProtoPropertySet_WithProperties_ReturnsCorrectProtoPropertySet()
    {
        var propertySet = new PropertySet
        {
            Properties = new Dictionary<string, PropertyValue>
            {
                { "key1", new PropertyValue { Type = DataType.String, Value = "value1" } },
                { "key2", new PropertyValue { Type = DataType.Int32, Value = 123 } }
            }
        };
        var result = propertySet.ToProtoPropertySet();
        Assert.NotNull(result);
        Assert.Equal(2, result.Keys.Count);
        Assert.Equal(2, result.Values.Count);
        Assert.Contains("key1", result.Keys);
        Assert.Contains("key2", result.Keys);
    }

    [Fact]
    public void ToProtoPropertySetList_NullList_ThrowsArgumentNullException()
    {
        PropertySetList propertySetList = null!;
        Assert.Throws<ArgumentNullException>(() => propertySetList.ToProtoPropertySetList());
    }

    [Fact]
    public void ToProtoPropertySetList_WithPropertySets_ReturnsCorrectList()
    {
        var propertySetList = new PropertySetList
        {
            PropertySets =
            {
                new PropertySet
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "key1", new PropertyValue { Type = DataType.String, Value = "value1" } }
                    }
                }
            }
        };
        var result = propertySetList.ToProtoPropertySetList();
        Assert.NotNull(result);
        Assert.Single(result.Propertyset);
    }

    [Fact]
    public void ToPropertyValue_NullProtoProperty_ThrowsArgumentNullException()
    {
        ProtoPropertyValue protoProperty = null!;
        Assert.Throws<ArgumentNullException>(() => protoProperty.ToPropertyValue());
    }

    [Fact]
    public void ToPropertyValue_ProtoInt32Property_ReturnsCorrectPropertyValue()
    {
        var protoProperty = new ProtoPropertyValue
        {
            Type = (uint)DataType.Int32,
            IsNull = false,
            IntValue = 42
        };
        var result = protoProperty.ToPropertyValue();
        Assert.NotNull(result);
        Assert.Equal(DataType.Int32, result.Type);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ToPropertyValue_ProtoStringProperty_ReturnsCorrectPropertyValue()
    {
        var protoProperty = new ProtoPropertyValue
        {
            Type = (uint)DataType.String,
            IsNull = false,
            StringValue = "test string"
        };
        var result = protoProperty.ToPropertyValue();
        Assert.NotNull(result);
        Assert.Equal(DataType.String, result.Type);
        Assert.Equal("test string", result.Value);
    }

    [Fact]
    public void ToPropertyValue_ProtoDoubleProperty_ReturnsCorrectPropertyValue()
    {
        var protoProperty = new ProtoPropertyValue
        {
            Type = (uint)DataType.Double,
            IsNull = false,
            DoubleValue = 3.14159
        };
        var result = protoProperty.ToPropertyValue();
        Assert.NotNull(result);
        Assert.Equal(DataType.Double, result.Type);
        Assert.Equal(3.14159, Convert.ToDouble(result.Value), 5);
    }

    [Fact]
    public void ToPropertyValue_ProtoNullValue_ReturnsPropertyWithNullValue()
    {
        var protoProperty = new ProtoPropertyValue
        {
            Type = (uint)DataType.String,
            IsNull = true
        };
        var result = protoProperty.ToPropertyValue();
        Assert.NotNull(result);
        Assert.Equal(DataType.String, result.Type);
        Assert.Null(result.Value);
    }

    [Fact]
    public void ToPropertyValue_ProtoEnumTypeNotSupported_ThrowsNotSupportedException()
    {
        var protoProperty = new ProtoPropertyValue
        {
            Type = (uint)DataType.UUID,
            IsNull = false
        };
        Assert.Throws<NotSupportedException>(() => protoProperty.ToPropertyValue());
    }

    [Fact]
    public void ToPropertyValue_ProtoBooleanProperty_ReturnsCorrectPropertyValue()
    {
        var protoProperty = new ProtoPropertyValue
        {
            Type = (uint)DataType.Boolean,
            IsNull = false,
            BooleanValue = true
        };
        var result = protoProperty.ToPropertyValue();
        Assert.NotNull(result);
        Assert.Equal(DataType.Boolean, result.Type);
        Assert.Equal(true, result.Value);
    }

    [Fact]
    public void ToPropertyValue_ProtoDateTimeProperty_ReturnsCorrectPropertyValue()
    {
        var now = DateTime.Now;
        var protoProperty = new ProtoPropertyValue
        {
            Type = (uint)DataType.DateTime,
            IsNull = false,
            LongValue = (ulong)now.Ticks
        };
        var result = protoProperty.ToPropertyValue();
        Assert.NotNull(result);
        Assert.Equal(DataType.DateTime, result.Type);
        Assert.NotNull(result.Value);
        Assert.Equal(now.Ticks, Convert.ToInt64(result.Value));
    }

    [Fact]
    public void ToPropertySet_NullProtoPropertySet_ThrowsArgumentNullException()
    {
        ProtoPropertySet protoPropertySet = null!;
        Assert.Throws<ArgumentNullException>(() => protoPropertySet.ToPropertySet());
    }

    [Fact]
    public void ToPropertySet_WithProtoProperties_ReturnsCorrectPropertySet()
    {
        var protoPropertySet = new ProtoPropertySet();
        protoPropertySet.Keys.Add("key1");
        protoPropertySet.Values.Add(new ProtoPropertyValue
        {
            Type = (uint)DataType.String,
            IsNull = false,
            StringValue = "value1"
        });
        protoPropertySet.Keys.Add("key2");
        protoPropertySet.Values.Add(new ProtoPropertyValue
        {
            Type = (uint)DataType.Int32,
            IsNull = false,
            IntValue = 123
        });
        var result = protoPropertySet.ToPropertySet();
        Assert.NotNull(result);
        Assert.Equal(2, result.Properties.Count);
        Assert.Contains("key1", result.Properties.Keys);
        Assert.Contains("key2", result.Properties.Keys);
        Assert.Equal(DataType.String, result.Properties["key1"].Type);
        Assert.Equal(DataType.Int32, result.Properties["key2"].Type);
    }

    [Fact]
    public void RoundTrip_PropertyValue_ShouldMaintainValue()
    {
        var original = new PropertyValue { Type = DataType.String, Value = "round trip test" };
        var proto = original.ToProtoPropertyValue();
        var result = proto.ToPropertyValue();
        Assert.Equal(original.Type, result.Type);
        Assert.Equal(original.Value, result.Value);
    }

    [Fact]
    public void RoundTrip_BooleanPropertyValue_ShouldMaintainValue()
    {
        var original = new PropertyValue { Type = DataType.Boolean, Value = true };
        var proto = original.ToProtoPropertyValue();
        var result = proto.ToPropertyValue();
        Assert.Equal(original.Type, result.Type);
        Assert.Equal(original.Value, result.Value);
    }

    [Fact]
    public void RoundTrip_DateTimePropertyValue_ShouldMaintainValue()
    {
        var original = new PropertyValue { Type = DataType.DateTime, Value = DateTime.Now.Ticks };
        var proto = original.ToProtoPropertyValue();
        var result = proto.ToPropertyValue();
        Assert.Equal(original.Type, result.Type);
        Assert.Equal(original.Value, result.Value);
    }

    [Fact]
    public void RoundTrip_PropertySet_ShouldMaintainProperties()
    {
        var original = new PropertySet
        {
            Properties = new Dictionary<string, PropertyValue>
            {
                { "key1", new PropertyValue { Type = DataType.String, Value = "value1" } },
                { "key2", new PropertyValue { Type = DataType.Int32, Value = 123 } }
            }
        };
        var proto = original.ToProtoPropertySet();
        var result = proto.ToPropertySet();
        Assert.Equal(original.Properties.Count, result.Properties.Count);
        foreach (var key in original.Properties.Keys)
        {
            Assert.Contains(key, result.Properties.Keys);
            Assert.Equal(original.Properties[key].Type, result.Properties[key].Type);
            Assert.Equal(original.Properties[key].Value, result.Properties[key].Value);
        }
    }

    [Fact]
    public void ToProtoPropertyValue_NestedPropertySet_ConvertsCorrectly()
    {
        var nestedPropertySet = new PropertySet
        {
            Properties = new Dictionary<string, PropertyValue>
            {
                { "nestedKey", new PropertyValue { Type = DataType.String, Value = "nestedValue" } }
            }
        };
        var property = new PropertyValue { Type = DataType.PropertySet, Value = nestedPropertySet };
        var protoProperty = property.ToProtoPropertyValue();
        Assert.NotNull(protoProperty);
        Assert.Equal((uint)DataType.PropertySet, protoProperty.Type);
        Assert.NotNull(protoProperty.PropertysetValue);
        Assert.Single(protoProperty.PropertysetValue.Keys);
        Assert.Equal("nestedKey", protoProperty.PropertysetValue.Keys[0]);
    }
}