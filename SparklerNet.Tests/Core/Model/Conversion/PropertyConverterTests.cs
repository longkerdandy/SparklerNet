using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;
using ProtoPropertyValue = SparklerNet.Core.Protobuf.Payload.Types.PropertyValue;
using ProtoPropertySet = SparklerNet.Core.Protobuf.Payload.Types.PropertySet;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class PropertyConverterTests
{
    [Theory]
    [InlineData(true)] // Test null PropertyValue
    [InlineData(false)] // Test null ProtoPropertyValue
    public void NullInput_ThrowsArgumentNullException(bool isPropertyValueTest)
    {
        if (isPropertyValueTest)
        {
            PropertyValue property = null!;
            Assert.Throws<ArgumentNullException>(() => property.ToProtoPropertyValue());
        }
        else
        {
            ProtoPropertyValue protoProperty = null!;
            Assert.Throws<ArgumentNullException>(() => protoProperty.ToPropertyValue());
        }
    }

    [Theory]
    [InlineData(true)] // Test null PropertySet
    [InlineData(false)] // Test null ProtoPropertySet
    [InlineData(null)] // Test null PropertySetList
    public void NullCollectionInput_ThrowsArgumentNullException(bool? isPropertySetTest)
    {
        if (isPropertySetTest.HasValue)
        {
            if (isPropertySetTest.Value)
            {
                PropertySet propertySet = null!;
                Assert.Throws<ArgumentNullException>(() => propertySet.ToProtoPropertySet());
            }
            else
            {
                ProtoPropertySet protoPropertySet = null!;
                Assert.Throws<ArgumentNullException>(() => protoPropertySet.ToPropertySet());
            }
        }
        else
        {
            PropertySetList propertySetList = null!;
            Assert.Throws<ArgumentNullException>(() => propertySetList.ToProtoPropertySetList());
        }
    }

    [Theory]
    [InlineData(DataType.Int32, 42)]
    [InlineData(DataType.String, "test string")]
    [InlineData(DataType.Boolean, true)]
    public void ToProtoPropertyValue_SupportedValueTypes_ReturnsCorrectProtoProperty(DataType dataType, object value)
    {
        var property = new PropertyValue { Type = dataType, Value = value };
        var result = property.ToProtoPropertyValue();

        Assert.NotNull(result);
        Assert.Equal((uint)dataType, result.Type);
        Assert.False(result.IsNull);

        // Verify the appropriate value based on the data type
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (dataType)
        {
            case DataType.Int32:
                Assert.Equal((uint)Convert.ToInt32(value), result.IntValue);
                break;
            case DataType.String:
                Assert.Equal((string)value, result.StringValue);
                break;
            case DataType.Boolean:
                Assert.Equal((bool)value, result.BooleanValue);
                break;
        }
    }

    [Theory]
    [InlineData(DataType.Double, 3.14159)]
    public void ToProtoPropertyValue_DoubleType_ReturnsCorrectProtoProperty(DataType dataType, double value)
    {
        var property = new PropertyValue { Type = dataType, Value = value };
        var result = property.ToProtoPropertyValue();

        Assert.NotNull(result);
        Assert.Equal((uint)dataType, result.Type);
        Assert.Equal(value, result.DoubleValue, 5);
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
    public void ToProtoPropertyValue_NullValue_ReturnsProtoPropertyWithNullFlag()
    {
        var property = new PropertyValue { Type = DataType.String, Value = null };
        var result = property.ToProtoPropertyValue();

        Assert.NotNull(result);
        Assert.Equal((uint)DataType.String, result.Type);
        Assert.True(result.IsNull);
    }

    [Theory]
    [InlineData(DataType.UUID, true)] // Test PropertyValue to ProtoPropertyValue
    [InlineData(DataType.UUID, false)] // Test ProtoPropertyValue to PropertyValue
    public void UnsupportedDataType_ThrowsNotSupportedException(DataType dataType, bool isToProtoTest)
    {
        if (isToProtoTest)
        {
            var property = new PropertyValue { Type = dataType, Value = new byte[16] };
            Assert.Throws<NotSupportedException>(() => property.ToProtoPropertyValue());
        }
        else
        {
            var protoProperty = new ProtoPropertyValue
            {
                Type = (uint)dataType,
                IsNull = false
            };
            Assert.Throws<NotSupportedException>(() => protoProperty.ToPropertyValue());
        }
    }

    [Theory]
    [InlineData(DataType.Int32, 42)]
    [InlineData(DataType.String, "test string")]
    [InlineData(DataType.Boolean, true)]
    public void ToPropertyValue_SupportedProtoTypes_ReturnsCorrectPropertyValue(DataType dataType, object value)
    {
        var protoProperty = new ProtoPropertyValue
        {
            Type = (uint)dataType,
            IsNull = false
        };

        // Set the appropriate value based on data type
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (dataType)
        {
            case DataType.Int32:
                protoProperty.IntValue = (uint)Convert.ToInt32(value);
                break;
            case DataType.String:
                protoProperty.StringValue = (string)value;
                break;
            case DataType.Boolean:
                protoProperty.BooleanValue = (bool)value;
                break;
        }

        var result = protoProperty.ToPropertyValue();

        Assert.NotNull(result);
        Assert.Equal(dataType, result.Type);
        Assert.Equal(value, result.Value);
    }

    [Theory]
    [InlineData(3.14159)]
    public void ToPropertyValue_DoubleProtoType_ReturnsCorrectPropertyValue(double value)
    {
        var protoProperty = new ProtoPropertyValue
        {
            Type = (uint)DataType.Double,
            IsNull = false,
            DoubleValue = value
        };

        var result = protoProperty.ToPropertyValue();

        Assert.NotNull(result);
        Assert.Equal(DataType.Double, result.Type);
        Assert.Equal(value, Convert.ToDouble(result.Value), 5);
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

    [Theory]
    [InlineData(1)] // Test with 1 property
    [InlineData(2)] // Test with 2 properties
    public void ToProtoPropertySet_WithProperties_ReturnsCorrectProtoPropertySet(int propertyCount)
    {
        var propertySet = new PropertySet
        {
            Properties = new Dictionary<string, PropertyValue>()
        };

        // Add properties based on count
        for (var i = 1; i <= propertyCount; i++)
            propertySet.Properties.Add(
                $"key{i}",
                new PropertyValue { Type = DataType.String, Value = $"value{i}" }
            );

        var result = propertySet.ToProtoPropertySet();

        Assert.NotNull(result);
        Assert.Equal(propertyCount, result.Keys.Count);
        Assert.Equal(propertyCount, result.Values.Count);

        // Verify all keys are present
        for (var i = 1; i <= propertyCount; i++) Assert.Contains($"key{i}", result.Keys);
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

    [Theory]
    [InlineData(DataType.String, "round trip test")]
    [InlineData(DataType.Boolean, true)]
    public void RoundTrip_PropertyValue_ShouldMaintainValue(DataType dataType, object value)
    {
        var original = new PropertyValue { Type = dataType, Value = value };
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