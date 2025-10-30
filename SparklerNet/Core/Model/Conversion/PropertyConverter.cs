using ProtoPropertyValue = SparklerNet.Core.Protobuf.Payload.Types.PropertyValue;
using ProtoPropertySet = SparklerNet.Core.Protobuf.Payload.Types.PropertySet;
using ProtoPropertySetList = SparklerNet.Core.Protobuf.Payload.Types.PropertySetList;

namespace SparklerNet.Core.Model.Conversion;

/// <summary>
///     Converts between <see cref="PropertyValue" /> and <see cref="ProtoPropertyValue" />.
/// </summary>
public static class PropertyConverter
{
    /// <summary>
    ///     Converts a <see cref="PropertyValue" /> to a Protobuf <see cref="ProtoPropertyValue" />.
    /// </summary>
    /// <param name="property">The property value to convert.</param>
    /// <returns>The converted Protobuf property value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="property" /> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the property data type is not supported.</exception>
    public static ProtoPropertyValue ToProtoPropertyValue(this PropertyValue property)
    {
        ArgumentNullException.ThrowIfNull(property);

        var protoProperty = new ProtoPropertyValue
        {
            Type = (uint)property.Type,
            IsNull = property.IsNull
        };

        if (property.IsNull) return protoProperty;
        // Use switch expression with separate cases for each enum value

        Action convertValue = property.Type switch
        {
            DataType.Int8 or DataType.Int16 or DataType.Int32 or DataType.UInt8 or DataType.UInt16 or DataType.UInt32 =>
                () => protoProperty.IntValue = Convert.ToUInt32(property.Value),
            DataType.Int64 or DataType.UInt64 => () => protoProperty.LongValue = Convert.ToUInt64(property.Value),
            DataType.Float => () => protoProperty.FloatValue = Convert.ToSingle(property.Value),
            DataType.Double => () => protoProperty.DoubleValue = Convert.ToDouble(property.Value),
            DataType.Boolean => () => protoProperty.BooleanValue = Convert.ToBoolean(property.Value),
            DataType.DateTime => () => protoProperty.LongValue = property.Value is long
                ? Convert.ToUInt64(property.Value)
                : throw new NotSupportedException("Value for DateTime type must be long"),
            DataType.String or DataType.Text => () => protoProperty.StringValue = property.Value!.ToString()!,
            DataType.PropertySet when property.Value is PropertySet propertySet =>
                () => protoProperty.PropertysetValue = propertySet.ToProtoPropertySet(),
            DataType.PropertySetList when property.Value is PropertySetList propertySetList =>
                () => protoProperty.PropertysetsValue = propertySetList.ToProtoPropertySetList(),
            _ => throw new NotSupportedException($"Data type {property.Type} is not supported in Property conversion.")
        };

        // Execute the conversion action
        convertValue();

        return protoProperty;
    }

    /// <summary>
    ///     Converts a <see cref="PropertySet" /> to a Protobuf <see cref="ProtoPropertySet" />.
    /// </summary>
    /// <param name="propertySet">The property set to convert.</param>
    /// <returns>The converted Protobuf property set.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertySet" /> is null.</exception>
    public static ProtoPropertySet ToProtoPropertySet(this PropertySet propertySet)
    {
        ArgumentNullException.ThrowIfNull(propertySet);

        var protoPropertySet = new ProtoPropertySet();

        foreach (var (key, value) in propertySet.Properties)
        {
            protoPropertySet.Keys.Add(key);
            protoPropertySet.Values.Add(value.ToProtoPropertyValue());
        }

        return protoPropertySet;
    }

    /// <summary>
    ///     Converts a <see cref="PropertySetList" /> to a Protobuf <see cref="ProtoPropertySetList" />.
    /// </summary>
    /// <param name="propertySetList">The property set list to convert.</param>
    /// <returns>The converted Protobuf property set list.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertySetList" /> is null.</exception>
    public static ProtoPropertySetList ToProtoPropertySetList(this PropertySetList propertySetList)
    {
        ArgumentNullException.ThrowIfNull(propertySetList);

        var protoPropertySetList = new ProtoPropertySetList();

        foreach (var propertySet in propertySetList.PropertySets)
            protoPropertySetList.Propertyset.Add(propertySet.ToProtoPropertySet());

        return protoPropertySetList;
    }

    /// <summary>
    ///     Converts a Protobuf <see cref="ProtoPropertyValue" /> to a <see cref="PropertyValue" />.
    /// </summary>
    /// <param name="protoProperty">The Protobuf property value to convert.</param>
    /// <returns>The converted property value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protoProperty" /> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the property data type is not supported.</exception>
    public static PropertyValue ToPropertyValue(this ProtoPropertyValue protoProperty)
    {
        ArgumentNullException.ThrowIfNull(protoProperty);

        var dataType = (DataType)protoProperty.Type;

        if (protoProperty.IsNull)
            return new PropertyValue
            {
                Type = dataType,
                Value = null
            };

        // Convert the value based on the data type.
        object? value = dataType switch
        {
            DataType.Int8 => (sbyte)protoProperty.IntValue,
            DataType.Int16 => (short)protoProperty.IntValue,
            DataType.Int32 => (int)protoProperty.IntValue,
            DataType.UInt8 => (byte)protoProperty.IntValue,
            DataType.UInt16 => (ushort)protoProperty.IntValue,
            DataType.UInt32 => protoProperty.IntValue,
            DataType.Int64 => (long)protoProperty.LongValue,
            DataType.UInt64 => protoProperty.LongValue,
            DataType.Float => protoProperty.FloatValue,
            DataType.Double => protoProperty.DoubleValue,
            DataType.Boolean => protoProperty.BooleanValue,
            DataType.DateTime => (long)protoProperty.LongValue,
            DataType.String or DataType.Text => protoProperty.StringValue,
            DataType.PropertySet => protoProperty.PropertysetValue.ToPropertySet(),
            DataType.PropertySetList => protoProperty.PropertysetsValue.ToPropertySetList(),
            _ => throw new NotSupportedException($"Data type {dataType} is not supported in PropertyValue conversion.")
        };

        return new PropertyValue
        {
            Type = dataType,
            Value = value
        };
    }

    /// <summary>
    ///     Converts a Protobuf <see cref="ProtoPropertySet" /> to a <see cref="PropertySet" />.
    /// </summary>
    /// <param name="protoPropertySet">The Protobuf property set to convert.</param>
    /// <returns>The converted property set.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protoPropertySet" /> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the keys and values arrays in <paramref name="protoPropertySet" /> have
    ///     different lengths.
    /// </exception>
    public static PropertySet ToPropertySet(this ProtoPropertySet protoPropertySet)
    {
        ArgumentNullException.ThrowIfNull(protoPropertySet);

        var propertySet = new PropertySet();

        // Ensure that the keys and values arrays have the same length.
        // Throws ArgumentException if the lengths do not match.
        if (protoPropertySet.Keys.Count != protoPropertySet.Values.Count)
            throw new ArgumentException("PropertySet keys and values arrays must have the same length.");

        for (var i = 0; i < protoPropertySet.Keys.Count; i++)
            propertySet.Properties[protoPropertySet.Keys[i]] = protoPropertySet.Values[i].ToPropertyValue();

        return propertySet;
    }

    /// <summary>
    ///     Converts a Protobuf <see cref="ProtoPropertySetList" /> to a <see cref="PropertySetList" />.
    /// </summary>
    /// <param name="protoPropertySetList">The Protobuf property set list to convert.</param>
    /// <returns>The converted property set list.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protoPropertySetList" /> is null.</exception>
    // ReSharper disable once MemberCanBePrivate.Global
    public static PropertySetList ToPropertySetList(this ProtoPropertySetList protoPropertySetList)
    {
        ArgumentNullException.ThrowIfNull(protoPropertySetList);

        var propertySetList = new PropertySetList();

        foreach (var protoPropertySet in protoPropertySetList.Propertyset)
            propertySetList.PropertySets.Add(protoPropertySet.ToPropertySet());

        return propertySetList;
    }
}