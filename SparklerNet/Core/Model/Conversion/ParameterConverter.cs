using JetBrains.Annotations;
using ProtoParameter = SparklerNet.Core.Protobuf.Payload.Types.Template.Types.Parameter;

namespace SparklerNet.Core.Model.Conversion;

/// <summary>
///     Converts between <see cref="Parameter" /> and <see cref="ProtoParameter" />.
/// </summary>
[PublicAPI]
public static class ParameterConverter
{
    /// <summary>
    ///     Converts a <see cref="Parameter" /> to a Protobuf <see cref="ProtoParameter" />.
    /// </summary>
    /// <param name="parameter">The parameter to convert.</param>
    /// <returns>The converted Protobuf parameter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameter" /> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the parameter data type is not supported.</exception>
    public static ProtoParameter ToProtoParameter(this Parameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        var protoParameter = new ProtoParameter
        {
            Name = parameter.Name,
            Type = (uint)parameter.Type
        };

        // Only set the value if it's not null
        if (parameter.Value != null)
        {
            // Use switch expression with separate cases for each enum value
            Action convertValue = parameter.Type switch
            {
                DataType.Int8 => () => protoParameter.IntValue = Convert.ToUInt32(parameter.Value),
                DataType.Int16 => () => protoParameter.IntValue = Convert.ToUInt32(parameter.Value),
                DataType.Int32 => () => protoParameter.IntValue = Convert.ToUInt32(parameter.Value),
                DataType.UInt8 => () => protoParameter.IntValue = Convert.ToUInt32(parameter.Value),
                DataType.UInt16 => () => protoParameter.IntValue = Convert.ToUInt32(parameter.Value),
                DataType.UInt32 => () => protoParameter.IntValue = Convert.ToUInt32(parameter.Value),
                DataType.Int64 => () => protoParameter.LongValue = Convert.ToUInt64(parameter.Value),
                DataType.UInt64 => () => protoParameter.LongValue = Convert.ToUInt64(parameter.Value),
                DataType.Float => () => protoParameter.FloatValue = Convert.ToSingle(parameter.Value),
                DataType.Double => () => protoParameter.DoubleValue = Convert.ToDouble(parameter.Value),
                DataType.Boolean => () => protoParameter.BooleanValue = Convert.ToBoolean(parameter.Value),
                DataType.DateTime => () => protoParameter.LongValue = Convert.ToUInt64(parameter.Value),
                DataType.String or DataType.Text => () => protoParameter.StringValue = parameter.Value.ToString()!,
                _ => throw new NotSupportedException($"Data type {parameter.Type} is not supported in Parameter.")
            };

            // Execute the conversion action
            convertValue();
        }

        return protoParameter;
    }

    /// <summary>
    ///     Converts a Protobuf <see cref="ProtoParameter" /> to a <see cref="Parameter" />.
    /// </summary>
    /// <param name="protoParameter">The Protobuf parameter to convert.</param>
    /// <returns>The converted parameter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protoParameter" /> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the parameter data type is not supported.</exception>
    public static Parameter ToParameter(this ProtoParameter protoParameter)
    {
        ArgumentNullException.ThrowIfNull(protoParameter);

        var dataType = (DataType)protoParameter.Type;

        // Convert the value based on the data type.
        object? value = null;
        if (protoParameter.ValueCase != ProtoParameter.ValueOneofCase.None)
            value = dataType switch
            {
                DataType.Int8 => (sbyte)protoParameter.IntValue,
                DataType.Int16 => (short)protoParameter.IntValue,
                DataType.Int32 => (int)protoParameter.IntValue,
                DataType.UInt8 => (byte)protoParameter.IntValue,
                DataType.UInt16 => (ushort)protoParameter.IntValue,
                DataType.UInt32 => protoParameter.IntValue,
                DataType.Int64 => (long)protoParameter.LongValue,
                DataType.UInt64 => protoParameter.LongValue,
                DataType.Float => protoParameter.FloatValue,
                DataType.Double => protoParameter.DoubleValue,
                DataType.Boolean => protoParameter.BooleanValue,
                DataType.DateTime => (long)protoParameter.LongValue,
                DataType.String or DataType.Text => protoParameter.StringValue,
                _ => throw new NotSupportedException($"Data type {dataType} is not supported in Parameter conversion.")
            };

        // Create a new Parameter with the name, type and value
        var parameter = new Parameter
        {
            Name = protoParameter.Name,
            Type = dataType,
            Value = value
        };

        return parameter;
    }
}