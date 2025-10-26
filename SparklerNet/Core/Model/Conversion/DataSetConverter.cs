using JetBrains.Annotations;
using ProtoDataSet = SparklerNet.Core.Protobuf.Payload.Types.DataSet;
using ProtoDataSetRow = SparklerNet.Core.Protobuf.Payload.Types.DataSet.Types.Row;
using ProtoDataSetValue = SparklerNet.Core.Protobuf.Payload.Types.DataSet.Types.DataSetValue;

namespace SparklerNet.Core.Model.Conversion;

/// <summary>
///     Converts between <see cref="DataSet" /> and <see cref="ProtoDataSet" />.
/// </summary>
[PublicAPI]
public static class DataSetConverter
{
    /// <summary>
    ///     Converts a <see cref="DataSet" /> to a Protobuf <see cref="ProtoDataSet" />.
    /// </summary>
    /// <param name="dataSet">The DataSet to convert.</param>
    /// <returns>The converted Protobuf DataSet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataSet" /> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the data type is not supported in DataSet conversion.</exception>
    public static ProtoDataSet ToProtoDataSet(this DataSet dataSet)
    {
        ArgumentNullException.ThrowIfNull(dataSet);

        var protoDataSet = new ProtoDataSet
        {
            NumOfColumns = (ulong)dataSet.ColumnCount
        };

        // Add column names
        foreach (var columnName in dataSet.Columns) protoDataSet.Columns.Add(columnName);

        // Add data types (convert DataType enum to uint32)
        foreach (var dataType in dataSet.Types) protoDataSet.Types_.Add((uint)dataType);

        // Convert rows
        foreach (var rowData in dataSet.GetRows())
        {
            var protoRow = new ProtoDataSetRow();
            for (var i = 0; i < rowData.Count; i++)
            {
                var value = rowData[i];
                var dataType = i < dataSet.Types.Count ? dataSet.Types[i] : DataType.Unknown;
                var protoValue = ConvertToProtoDataSetValue(value, dataType);
                protoRow.Elements.Add(protoValue);
            }

            protoDataSet.Rows.Add(protoRow);
        }

        return protoDataSet;
    }

    /// <summary>
    ///     Converts an .NET object to a Protobuf DataSetValue based on the specified data type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="dataType">The data type of the value.</param>
    /// <returns>The converted Protobuf DataSetValue.</returns>
    /// <exception cref="NotSupportedException">Thrown when the data type is not supported in DataSet conversion.</exception>
    private static ProtoDataSetValue ConvertToProtoDataSetValue(object value, DataType dataType)
    {
        // Convert the value based on the data type
        return dataType switch
        {
            DataType.Int8 or DataType.Int16 or DataType.Int32 or DataType.UInt8 or DataType.UInt16 or DataType.UInt32 => 
                new ProtoDataSetValue { IntValue = Convert.ToUInt32(value) },
            DataType.Int64 or DataType.UInt64 => new ProtoDataSetValue { LongValue = Convert.ToUInt64(value) },
            DataType.Float => new ProtoDataSetValue { FloatValue = Convert.ToSingle(value) },
            DataType.Double => new ProtoDataSetValue { DoubleValue = Convert.ToDouble(value) },
            DataType.Boolean => new ProtoDataSetValue { BooleanValue = Convert.ToBoolean(value) },
            DataType.DateTime => new ProtoDataSetValue { LongValue = Convert.ToUInt64(value) },
            DataType.String or DataType.Text => new ProtoDataSetValue { StringValue = value.ToString()! },
            _ => throw new NotSupportedException($"Data type {dataType} is not supported in DataSet conversion.")
        };
    }

    /// <summary>
    ///     Converts a Protobuf <see cref="ProtoDataSet" /> to a <see cref="DataSet" />.
    /// </summary>
    /// <param name="protoDataSet">The Protobuf DataSet to convert.</param>
    /// <returns>The converted DataSet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protoDataSet" /> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the data type is not supported in DataSet conversion.</exception>
    public static DataSet ToDataSet(this ProtoDataSet protoDataSet)
    {
        ArgumentNullException.ThrowIfNull(protoDataSet);

        var dataSet = new DataSet
        {
            Columns = protoDataSet.Columns.ToList(),
            Types = protoDataSet.Types_.Select(type => (DataType)type).ToList(),
            ColumnData = []
        };

        // Initialize ColumnData dictionary
        foreach (var columnName in dataSet.Columns) dataSet.ColumnData[columnName] = [];

        // Process each row
        foreach (var protoRow in protoDataSet.Rows)
            for (var i = 0; i < protoRow.Elements.Count; i++)
            {
                if (i >= dataSet.Columns.Count)
                    continue; // Skip if there are more elements than columns

                var columnName = dataSet.Columns[i];
                var protoValue = protoRow.Elements[i];
                var dataType = i < dataSet.Types.Count ? dataSet.Types[i] : DataType.Unknown;
                var value = ConvertFromProtoDataSetValue(protoValue, dataType);
                dataSet.ColumnData[columnName].Add(value);
            }

        return dataSet;
    }

    /// <summary>
    ///     Converts a Protobuf DataSetValue to a .NET object based on the specified data type.
    /// </summary>
    /// <param name="protoValue">The Protobuf DataSetValue to convert.</param>
    /// <param name="dataType">The target data type.</param>
    /// <returns>The converted .NET object.</returns>
    /// <exception cref="NotSupportedException">Thrown when the data type is not supported in DataSet conversion.</exception>
    private static object ConvertFromProtoDataSetValue(ProtoDataSetValue protoValue, DataType dataType)
    {
        // Convert based on the data type
        return dataType switch
        {
            DataType.Int8 => (sbyte)protoValue.IntValue,
            DataType.Int16 => (short)protoValue.IntValue,
            DataType.Int32 => (int)protoValue.IntValue,
            DataType.UInt8 => (byte)protoValue.IntValue,
            DataType.UInt16 => (ushort)protoValue.IntValue,
            DataType.UInt32 => protoValue.IntValue,
            DataType.Int64 => (long)protoValue.LongValue,
            DataType.UInt64 => protoValue.LongValue,
            DataType.Float => protoValue.FloatValue,
            DataType.Double => protoValue.DoubleValue,
            DataType.Boolean => protoValue.BooleanValue,
            DataType.DateTime => (long)protoValue.LongValue,
            DataType.String or DataType.Text => protoValue.StringValue,
            _ => throw new NotSupportedException($"Data type {dataType} is not supported in DataSet conversion.")
        };
    }
}