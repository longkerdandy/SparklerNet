using JetBrains.Annotations;
using static Google.Protobuf.ByteString;
using ProtoMetric = SparklerNet.Core.Protobuf.Payload.Types.Metric;

namespace SparklerNet.Core.Model.Conversion;

/// <summary>
///     Converts between <see cref="Metric" /> and <see cref="ProtoMetric" />.
/// </summary>
[PublicAPI]
public static class MetricConverter
{
    /// <summary>
    ///     Converts a <see cref="Metric" /> to a Protobuf <see cref="ProtoMetric" />.
    /// </summary>
    /// <param name="metric">The Metric to convert.</param>
    /// <returns>The converted Protobuf Metric.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metric" /> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the metric data type is not supported.</exception>
    public static ProtoMetric ToProtoMetric(this Metric metric)
    {
        ArgumentNullException.ThrowIfNull(metric);

        var protoMetric = new ProtoMetric
        {
            // Set basic properties
            IsHistorical = metric.IsHistorical ?? false,
            IsTransient = metric.IsTransient ?? false,
            IsNull = metric.IsNull
        };

        // Set optional properties if they have values
        if (metric.Name != null) protoMetric.Name = metric.Name;
        if (metric.Alias.HasValue) protoMetric.Alias = metric.Alias.Value;
        if (metric.Timestamp.HasValue) protoMetric.Timestamp = (ulong)metric.Timestamp.Value;
        if (metric.DateType.HasValue) protoMetric.Datatype = (uint)metric.DateType.Value;
        if (metric.Metadata != null) protoMetric.Metadata = metric.Metadata.ToProtoMetaData();
        if (metric.Properties != null) protoMetric.Properties = metric.Properties.ToProtoPropertySet();

        // Only set the value if it's not null and DateType is specified
        if (metric is { DateType: not null, IsNull: false })
        {
            // Set the value based on the data type
            Action valueAssignment = metric.DateType switch
            {
                DataType.Int8 => () => protoMetric.IntValue = Convert.ToUInt32(metric.Value),
                DataType.Int16 => () => protoMetric.IntValue = Convert.ToUInt32(metric.Value),
                DataType.Int32 => () => protoMetric.IntValue = Convert.ToUInt32(metric.Value),
                DataType.UInt8 => () => protoMetric.IntValue = Convert.ToUInt32(metric.Value),
                DataType.UInt16 => () => protoMetric.IntValue = Convert.ToUInt32(metric.Value),
                DataType.UInt32 => () => protoMetric.IntValue = Convert.ToUInt32(metric.Value),
                DataType.Int64 => () => protoMetric.LongValue = Convert.ToUInt64(metric.Value),
                DataType.UInt64 => () => protoMetric.LongValue = Convert.ToUInt64(metric.Value),
                DataType.Float => () => protoMetric.FloatValue = Convert.ToSingle(metric.Value),
                DataType.Double => () => protoMetric.DoubleValue = Convert.ToDouble(metric.Value),
                DataType.Boolean => () => protoMetric.BooleanValue = Convert.ToBoolean(metric.Value),
                DataType.DateTime => () => protoMetric.LongValue = Convert.ToUInt64(metric.Value),
                DataType.String or DataType.Text => () => protoMetric.StringValue = metric.Value!.ToString()!,
                DataType.UUID => () => protoMetric.StringValue = metric.Value!.ToString()!,
                DataType.Bytes or DataType.File => () => protoMetric.BytesValue = metric.Value is byte[] bytes
                    ? CopyFrom(bytes)
                    : throw new NotSupportedException("Value for Bytes/File type must be byte[]"),
                DataType.DataSet => () => protoMetric.DatasetValue = metric.Value is DataSet dataSet
                    ? dataSet.ToProtoDataSet()
                    : throw new NotSupportedException("Value for DataSet type must be dataset"),
                DataType.Template => () => protoMetric.TemplateValue = metric.Value is Template template
                    ? template.ToProtoTemplate()
                    : throw new NotSupportedException("Value for Template type must be template"),
                _ => throw new NotSupportedException(
                    $"Data type {metric.DateType} is not supported in Metric conversion")
            };

            // Execute the conversion action
            valueAssignment();
        }

        return protoMetric;
    }

    /// <summary>
    ///     Converts a Protobuf <see cref="ProtoMetric" /> to a <see cref="Metric" />.
    /// </summary>
    /// <param name="protoMetric">The Protobuf Metric to convert.</param>
    /// <returns>The converted Metric.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protoMetric" /> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the metric data type is not supported.</exception>
    public static Metric ToMetric(this ProtoMetric protoMetric)
    {
        ArgumentNullException.ThrowIfNull(protoMetric);

        var dataType = protoMetric.Datatype != 0 ? (DataType?)protoMetric.Datatype : null;

        // Create a new Metric with basic properties
        var metric = new Metric
        {
            Name = protoMetric.Name, // Will be null if not set
            Alias = protoMetric.Alias != 0 ? protoMetric.Alias : null,
            Timestamp = protoMetric.Timestamp != 0 ? (long?)protoMetric.Timestamp : null,
            DateType = dataType,
            IsHistorical = protoMetric.IsHistorical,
            IsTransient = protoMetric.IsTransient,
            Metadata = protoMetric.Metadata?.ToMetaData(),
            Properties = protoMetric.Properties?.ToPropertySet()
        };

        // Convert the value based on the data type if it's not null
        if (dataType != null && !protoMetric.IsNull)
            metric.Value = dataType switch
            {
                DataType.Int8 => (sbyte)protoMetric.IntValue,
                DataType.Int16 => (short)protoMetric.IntValue,
                DataType.Int32 => (int)protoMetric.IntValue,
                DataType.UInt8 => (byte)protoMetric.IntValue,
                DataType.UInt16 => (ushort)protoMetric.IntValue,
                DataType.UInt32 => protoMetric.IntValue,
                DataType.Int64 => (long)protoMetric.LongValue,
                DataType.UInt64 => protoMetric.LongValue,
                DataType.Float => protoMetric.FloatValue,
                DataType.Double => protoMetric.DoubleValue,
                DataType.Boolean => protoMetric.BooleanValue,
                DataType.DateTime => (long)protoMetric.LongValue,
                DataType.String or DataType.Text or DataType.UUID => protoMetric.StringValue,
                DataType.Bytes or DataType.File => protoMetric.BytesValue?.ToByteArray(),
                DataType.DataSet => protoMetric.DatasetValue?.ToDataSet(),
                DataType.Template => protoMetric.TemplateValue?.ToTemplate(),
                _ => throw new NotSupportedException($"Data type {dataType} is not supported in Metric conversion")
            };

        return metric;
    }
}