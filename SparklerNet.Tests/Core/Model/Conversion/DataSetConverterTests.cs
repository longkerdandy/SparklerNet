using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;
using ProtoDataSet = SparklerNet.Core.Protobuf.Payload.Types.DataSet;
using ProtoDataSetRow = SparklerNet.Core.Protobuf.Payload.Types.DataSet.Types.Row;
using ProtoDataSetValue = SparklerNet.Core.Protobuf.Payload.Types.DataSet.Types.DataSetValue;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class DataSetConverterTests
{
    [Fact]
    public void ToProtoDataSet_NullDataSet_ThrowsArgumentNullException()
    {
        DataSet dataSet = null!;
        Assert.Throws<ArgumentNullException>(() => dataSet.ToProtoDataSet());
    }

    [Fact]
    public void ToDataSet_NullProtoDataSet_ThrowsArgumentNullException()
    {
        ProtoDataSet protoDataSet = null!;
        Assert.Throws<ArgumentNullException>(() => protoDataSet.ToDataSet());
    }

    [Theory]
    [InlineData(DataType.Unknown, typeof(NotSupportedException))]
    [InlineData(DataType.Int32, null)]
    public void ToProtoDataSet_DataTypeHandling(DataType dataType, Type? expectedExceptionType)
    {
        var dataSet = new DataSet
        {
            Columns = ["TestColumn"],
            Types = [dataType],
            ColumnData = new Dictionary<string, List<object>>
            {
                { "TestColumn", [1] }
            }
        };

        if (expectedExceptionType != null)
        {
            Assert.Throws(expectedExceptionType, () => dataSet.ToProtoDataSet());
        }
        else
        {
            var result = dataSet.ToProtoDataSet();
            Assert.NotNull(result);
            Assert.Single(result.Rows);
        }
    }

    [Theory]
    [InlineData(DataType.Unknown, typeof(NotSupportedException))]
    [InlineData(DataType.Int32, null)]
    public void ToDataSet_DataTypeHandling(DataType dataType, Type? expectedExceptionType)
    {
        var protoDataSet = new ProtoDataSet
        {
            Columns = { "TestColumn" },
            Types_ = { (uint)dataType }
        };

        var row = new ProtoDataSetRow();
        row.Elements.Add(new ProtoDataSetValue { IntValue = 1 });
        protoDataSet.Rows.Add(row);

        if (expectedExceptionType != null)
        {
            Assert.Throws(expectedExceptionType, () => protoDataSet.ToDataSet());
        }
        else
        {
            var result = protoDataSet.ToDataSet();
            Assert.NotNull(result);
            Assert.Equal(1, result.RowCount);
        }
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 2, 2)] // 3 columns, 2 rows
    public void DataSetToProtoDataSet_EmptyAndBasicConversion(int columnCount, int rowCount, int expectedRowCount)
    {
        DataSet dataSet;

        // Configure columns and data if needed
        if (columnCount > 0)
        {
            var columnsList = Enumerable.Range(1, columnCount).Select(i => $"Column{i}").ToList();
            var typesList = Enumerable.Repeat(DataType.Int32, columnCount).ToList();
            dataSet = new DataSet
            {
                Columns = columnsList,
                Types = typesList,
                ColumnData = columnsList.ToDictionary(
                    col => col,
                    _ => Enumerable.Range(1, rowCount).Cast<object>().ToList()
                )
            };
        }
        else
        {
            dataSet = new DataSet();
        }

        var protoDataSet = dataSet.ToProtoDataSet();

        Assert.NotNull(protoDataSet);
        Assert.Equal((ulong)columnCount, protoDataSet.NumOfColumns);
        Assert.Equal(columnCount, protoDataSet.Columns.Count);
        Assert.Equal(columnCount, protoDataSet.Types_.Count);
        Assert.Equal(expectedRowCount, protoDataSet.Rows.Count);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 2)] // 3 columns, 2 rows
    public void ProtoDataSetToDataSet_EmptyAndBasicConversion(int columnCount, int rowCount)
    {
        var protoDataSet = new ProtoDataSet
        {
            NumOfColumns = (ulong)columnCount
        };

        // Configure columns and data if needed
        if (columnCount > 0)
        {
            for (var i = 1; i <= columnCount; i++)
            {
                protoDataSet.Columns.Add($"Column{i}");
                protoDataSet.Types_.Add((uint)DataType.Int32);
            }

            // Add rows
            for (var i = 0; i < rowCount; i++)
            {
                var row = new ProtoDataSetRow();
                for (var j = 0; j < columnCount; j++)
                    row.Elements.Add(new ProtoDataSetValue { IntValue = (uint)(i + 1) });
                protoDataSet.Rows.Add(row);
            }
        }

        var dataSet = protoDataSet.ToDataSet();

        Assert.NotNull(dataSet);
        Assert.Equal(columnCount, dataSet.ColumnCount);
        Assert.Equal(rowCount, dataSet.RowCount);
        Assert.Equal(columnCount, dataSet.Columns.Count);
        Assert.Equal(columnCount, dataSet.Types.Count);
    }

    [Fact]
    public void ToProtoDataSet_AllDataTypes_ConvertsCorrectly()
    {
        var dataSet = new DataSet
        {
            Columns =
            [
                "Int8", "Int16", "Int32", "Int64", "UInt8", "UInt16", "UInt32", "UInt64", "Float", "Double", "Boolean",
                "String"
            ],
            Types =
            [
                DataType.Int8, DataType.Int16, DataType.Int32, DataType.Int64, DataType.UInt8, DataType.UInt16,
                DataType.UInt32, DataType.UInt64, DataType.Float, DataType.Double, DataType.Boolean, DataType.String
            ],
            ColumnData = new Dictionary<string, List<object>>
            {
                { "Int8", [(sbyte)127] },
                { "Int16", [(short)32767] },
                { "Int32", [2147483647] },
                { "Int64", [9223372036854775807L] },
                { "UInt8", [(byte)255] },
                { "UInt16", [(ushort)65535] },
                { "UInt32", [4294967295u] },
                { "UInt64", [18446744073709551615UL] },
                { "Float", [3.14f] },
                { "Double", [2.71828] },
                { "Boolean", [true] },
                { "String", ["Test string"] }
            }
        };

        var protoDataSet = dataSet.ToProtoDataSet();

        Assert.NotNull(protoDataSet);
        Assert.Single(protoDataSet.Rows);
        var row = protoDataSet.Rows[0];
        Assert.Equal(12, row.Elements.Count);
        Assert.Equal(127u, row.Elements[0].IntValue);
        Assert.Equal(32767u, row.Elements[1].IntValue);
        Assert.Equal(2147483647u, row.Elements[2].IntValue);
        Assert.Equal(9223372036854775807UL, row.Elements[3].LongValue);
        Assert.Equal(255u, row.Elements[4].IntValue);
        Assert.Equal(65535u, row.Elements[5].IntValue);
        Assert.Equal(4294967295u, row.Elements[6].IntValue);
        Assert.Equal(18446744073709551615UL, row.Elements[7].LongValue);
        Assert.Equal(3.14f, row.Elements[8].FloatValue, 2);
        Assert.Equal(2.71828, row.Elements[9].DoubleValue, 5);
        Assert.True(row.Elements[10].BooleanValue);
        Assert.Equal("Test string", row.Elements[11].StringValue);
    }

    [Fact]
    public void ToDataSet_AllDataTypes_ConvertsCorrectly()
    {
        var protoDataSet = new ProtoDataSet
        {
            NumOfColumns = 12,
            Columns =
            {
                "Int8", "Int16", "Int32", "Int64", "UInt8", "UInt16", "UInt32", "UInt64", "Float", "Double", "Boolean",
                "String"
            },
            Types_ =
            {
                (uint)DataType.Int8, (uint)DataType.Int16, (uint)DataType.Int32, (uint)DataType.Int64,
                (uint)DataType.UInt8, (uint)DataType.UInt16, (uint)DataType.UInt32, (uint)DataType.UInt64,
                (uint)DataType.Float, (uint)DataType.Double, (uint)DataType.Boolean, (uint)DataType.String
            }
        };

        var row = new ProtoDataSetRow();
        row.Elements.Add(new ProtoDataSetValue { IntValue = 127 });
        row.Elements.Add(new ProtoDataSetValue { IntValue = 32767 });
        row.Elements.Add(new ProtoDataSetValue { IntValue = 2147483647 });
        row.Elements.Add(new ProtoDataSetValue { LongValue = 9223372036854775807 });
        row.Elements.Add(new ProtoDataSetValue { IntValue = 255 });
        row.Elements.Add(new ProtoDataSetValue { IntValue = 65535 });
        row.Elements.Add(new ProtoDataSetValue { IntValue = 4294967295 });
        row.Elements.Add(new ProtoDataSetValue { LongValue = 18446744073709551615UL });
        row.Elements.Add(new ProtoDataSetValue { FloatValue = 3.14f });
        row.Elements.Add(new ProtoDataSetValue { DoubleValue = 2.71828 });
        row.Elements.Add(new ProtoDataSetValue { BooleanValue = true });
        row.Elements.Add(new ProtoDataSetValue { StringValue = "Test string" });

        protoDataSet.Rows.Add(row);

        var dataSet = protoDataSet.ToDataSet();

        Assert.NotNull(dataSet);
        Assert.Equal(12, dataSet.ColumnCount);
        Assert.Equal(1, dataSet.RowCount);
        Assert.Equal((sbyte)127, dataSet.ColumnData["Int8"][0]);
        Assert.Equal((short)32767, dataSet.ColumnData["Int16"][0]);
        Assert.Equal(2147483647, dataSet.ColumnData["Int32"][0]);
        Assert.Equal(9223372036854775807L, dataSet.ColumnData["Int64"][0]);
        Assert.Equal((byte)255, dataSet.ColumnData["UInt8"][0]);
        Assert.Equal((ushort)65535, dataSet.ColumnData["UInt16"][0]);
        Assert.Equal(4294967295u, dataSet.ColumnData["UInt32"][0]);
        Assert.Equal(18446744073709551615UL, dataSet.ColumnData["UInt64"][0]);
        Assert.Equal(3.14f, (float)dataSet.ColumnData["Float"][0], 2);
        Assert.Equal(2.71828, (double)dataSet.ColumnData["Double"][0], 5);
        Assert.True((bool)dataSet.ColumnData["Boolean"][0]);
        Assert.Equal("Test string", dataSet.ColumnData["String"][0]);
    }

    [Theory]
    [InlineData(2, 3)] // 2 columns, 3 elements (extra element should be skipped)
    [InlineData(3, 2)] // 3 columns, 2 elements (missing element should be handled)
    public void ToDataSet_RowElementCountHandling(int columnCount, int elementCount)
    {
        var protoDataSet = new ProtoDataSet
        {
            NumOfColumns = (ulong)columnCount
        };

        // Add columns and types
        for (var i = 1; i <= columnCount; i++)
        {
            protoDataSet.Columns.Add($"Column{i}");
            protoDataSet.Types_.Add((uint)DataType.Int32);
        }

        // Add a row with different element count than column count
        var row = new ProtoDataSetRow();
        for (var i = 0; i < elementCount; i++) row.Elements.Add(new ProtoDataSetValue { IntValue = (uint)(i + 1) });
        protoDataSet.Rows.Add(row);

        var dataSet = protoDataSet.ToDataSet();

        Assert.NotNull(dataSet);
        Assert.Equal(columnCount, dataSet.ColumnCount);
        Assert.Equal(1, dataSet.RowCount);
    }

    [Theory]
    [InlineData(new object[] { "ID", "Name", "Value" },
        new object[] { DataType.Int32, DataType.String, DataType.Double }, 3)]
    [InlineData(new object[] { "SingleColumn" }, new object[] { DataType.Boolean }, 1)]
    public void DataSetRoundTrip_PreservesData(object[] columnNameObjects, object[] dataTypeObjects, int rowCount)
    {
        // Convert object[] to string[] and DataType[]
        var columnNames = columnNameObjects.Cast<string>().ToArray();
        var dataTypes = dataTypeObjects.Cast<DataType>().ToArray();

        // Create the original data set with sample data
        var originalDataSet = new DataSet
        {
            Columns = columnNames.ToList(),
            Types = dataTypes.ToList(),
            ColumnData = new Dictionary<string, List<object>>()
        };

        // Add sample data to each column
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        for (var colIndex = 0; colIndex < columnNames.Length; colIndex++)
        {
            var columnName = columnNames[colIndex];
            var dataType = dataTypes[colIndex];

            // Generate sample value based on the data type
            object value = dataType switch
            {
                DataType.Int32 => rowIndex + 1,
                DataType.String => $"Value_{rowIndex}",
                DataType.Double => rowIndex * 1.5,
                DataType.Boolean => rowIndex % 2 == 0,
                _ => 0
            };

            if (!originalDataSet.ColumnData.ContainsKey(columnName)) originalDataSet.ColumnData[columnName] = [];
            originalDataSet.ColumnData[columnName].Add(value);
        }

        // Round trip: DataSet -> ProtoDataSet -> DataSet
        var protoDataSet = originalDataSet.ToProtoDataSet();
        var roundTripDataSet = protoDataSet.ToDataSet();

        Assert.NotNull(roundTripDataSet);
        Assert.Equal(originalDataSet.ColumnCount, roundTripDataSet.ColumnCount);
        Assert.Equal(originalDataSet.RowCount, roundTripDataSet.RowCount);
        Assert.Equal(originalDataSet.Columns, roundTripDataSet.Columns);
        Assert.Equal(originalDataSet.Types, roundTripDataSet.Types);

        // Verify column data
        foreach (var columnName in originalDataSet.Columns)
        {
            Assert.Contains(columnName, roundTripDataSet.ColumnData);
            Assert.Equal(originalDataSet.ColumnData[columnName].Count, roundTripDataSet.ColumnData[columnName].Count);
            for (var i = 0; i < originalDataSet.ColumnData[columnName].Count; i++)
                Assert.Equal(originalDataSet.ColumnData[columnName][i], roundTripDataSet.ColumnData[columnName][i]);
        }
    }
}