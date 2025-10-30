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
    public void ToProtoDataSet_EmptyDataSet_ReturnsEmptyProtoDataSet()
    {
        var dataSet = new DataSet();
        var protoDataSet = dataSet.ToProtoDataSet();
        Assert.NotNull(protoDataSet);
        Assert.Equal(0ul, protoDataSet.NumOfColumns);
        Assert.Empty(protoDataSet.Columns);
        Assert.Empty(protoDataSet.Types_);
        Assert.Empty(protoDataSet.Rows);
    }

    [Fact]
    public void ToProtoDataSet_BasicDataSet_ConvertsCorrectly()
    {
        var dataSet = new DataSet
        {
            Columns = ["ID", "Name", "Active"],
            Types = [DataType.Int32, DataType.String, DataType.Boolean],
            ColumnData = new Dictionary<string, List<object>>
            {
                { "ID", [1, 2, 3] },
                { "Name", ["Alice", "Bob", "Charlie"] },
                { "Active", [true, false, true] }
            }
        };

        var protoDataSet = dataSet.ToProtoDataSet();

        Assert.NotNull(protoDataSet);
        Assert.Equal(3ul, protoDataSet.NumOfColumns);
        Assert.Equal(["ID", "Name", "Active"], protoDataSet.Columns.ToList());
        Assert.Equal([(uint)DataType.Int32, (uint)DataType.String, (uint)DataType.Boolean],
            protoDataSet.Types_.ToList());
        Assert.Equal(3, protoDataSet.Rows.Count);

        // Verify the first row
        Assert.Equal(3, protoDataSet.Rows[0].Elements.Count);
        Assert.Equal(1u, protoDataSet.Rows[0].Elements[0].IntValue);
        Assert.Equal("Alice", protoDataSet.Rows[0].Elements[1].StringValue);
        Assert.True(protoDataSet.Rows[0].Elements[2].BooleanValue);

        // Verify the second row
        Assert.Equal(2u, protoDataSet.Rows[1].Elements[0].IntValue);
        Assert.Equal("Bob", protoDataSet.Rows[1].Elements[1].StringValue);
        Assert.False(protoDataSet.Rows[1].Elements[2].BooleanValue);

        // Verify the third row
        Assert.Equal(3u, protoDataSet.Rows[2].Elements[0].IntValue);
        Assert.Equal("Charlie", protoDataSet.Rows[2].Elements[1].StringValue);
        Assert.True(protoDataSet.Rows[2].Elements[2].BooleanValue);
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
    public void ToProtoDataSet_UnsupportedDataType_ThrowsNotSupportedException()
    {
        var dataSet = new DataSet
        {
            Columns = ["Unsupported"],
            Types = [DataType.Unknown],
            ColumnData = new Dictionary<string, List<object>>
            {
                { "Unsupported", [1] }
            }
        };

        Assert.Throws<NotSupportedException>(() => dataSet.ToProtoDataSet());
    }

    [Fact]
    public void ToDataSet_NullProtoDataSet_ThrowsArgumentNullException()
    {
        ProtoDataSet protoDataSet = null!;
        Assert.Throws<ArgumentNullException>(() => protoDataSet.ToDataSet());
    }

    [Fact]
    public void ToDataSet_EmptyProtoDataSet_ReturnsEmptyDataSet()
    {
        var protoDataSet = new ProtoDataSet();
        var dataSet = protoDataSet.ToDataSet();
        Assert.NotNull(dataSet);
        Assert.Empty(dataSet.Columns);
        Assert.Empty(dataSet.Types);
        Assert.Empty(dataSet.ColumnData);
        Assert.Equal(0, dataSet.RowCount);
        Assert.Equal(0, dataSet.ColumnCount);
    }

    [Fact]
    public void ToDataSet_BasicProtoDataSet_ConvertsCorrectly()
    {
        var protoDataSet = new ProtoDataSet
        {
            NumOfColumns = 3,
            Columns = { "ID", "Name", "Active" },
            Types_ = { (uint)DataType.Int32, (uint)DataType.String, (uint)DataType.Boolean }
        };

        // Add rows
        var row1 = new ProtoDataSetRow();
        row1.Elements.Add(new ProtoDataSetValue { IntValue = 1 });
        row1.Elements.Add(new ProtoDataSetValue { StringValue = "Alice" });
        row1.Elements.Add(new ProtoDataSetValue { BooleanValue = true });

        var row2 = new ProtoDataSetRow();
        row2.Elements.Add(new ProtoDataSetValue { IntValue = 2 });
        row2.Elements.Add(new ProtoDataSetValue { StringValue = "Bob" });
        row2.Elements.Add(new ProtoDataSetValue { BooleanValue = false });

        var row3 = new ProtoDataSetRow();
        row3.Elements.Add(new ProtoDataSetValue { IntValue = 3 });
        row3.Elements.Add(new ProtoDataSetValue { StringValue = "Charlie" });
        row3.Elements.Add(new ProtoDataSetValue { BooleanValue = true });

        protoDataSet.Rows.Add(row1);
        protoDataSet.Rows.Add(row2);
        protoDataSet.Rows.Add(row3);

        var dataSet = protoDataSet.ToDataSet();

        Assert.NotNull(dataSet);
        Assert.Equal(3, dataSet.ColumnCount);
        Assert.Equal(3, dataSet.RowCount);
        Assert.Equal(["ID", "Name", "Active"], dataSet.Columns);
        Assert.Equal([DataType.Int32, DataType.String, DataType.Boolean], dataSet.Types);
        Assert.Equal([1, 2, 3], dataSet.ColumnData["ID"].Cast<int>());
        Assert.Equal(["Alice", "Bob", "Charlie"], dataSet.ColumnData["Name"].Cast<string>());
        Assert.Equal([true, false, true], dataSet.ColumnData["Active"].Cast<bool>());
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

    [Fact]
    public void ToDataSet_TooManyElementsInRow_SkipsExtraElements()
    {
        var protoDataSet = new ProtoDataSet
        {
            NumOfColumns = 2,
            Columns = { "Column1", "Column2" },
            Types_ = { (uint)DataType.Int32, (uint)DataType.String }
        };

        var row = new ProtoDataSetRow();
        row.Elements.Add(new ProtoDataSetValue { IntValue = 1 });
        row.Elements.Add(new ProtoDataSetValue { StringValue = "Test" });
        row.Elements.Add(new ProtoDataSetValue { BooleanValue = true }); // This should be skipped

        protoDataSet.Rows.Add(row);

        var dataSet = protoDataSet.ToDataSet();

        Assert.NotNull(dataSet);
        Assert.Equal(2, dataSet.ColumnCount);
        Assert.Equal(1, dataSet.RowCount);
        Assert.Equal(1, dataSet.ColumnData["Column1"][0]);
        Assert.Equal("Test", dataSet.ColumnData["Column2"][0]);
    }

    [Fact]
    public void ToDataSet_UnsupportedDataType_ThrowsNotSupportedException()
    {
        var protoDataSet = new ProtoDataSet
        {
            Columns = { "Unsupported" },
            Types_ = { (uint)DataType.Unknown }
        };

        // Add a row to ensure ConvertFromProtoDataSetValue is called
        var row = new ProtoDataSetRow();
        row.Elements.Add(new ProtoDataSetValue { IntValue = 1 });
        protoDataSet.Rows.Add(row);

        Assert.Throws<NotSupportedException>(() => protoDataSet.ToDataSet());
    }

    [Fact]
    public void DataSetRoundTrip_PreservesData()
    {
        var originalDataSet = new DataSet
        {
            Columns = ["ID", "Name", "Value"],
            Types = [DataType.Int32, DataType.String, DataType.Double],
            ColumnData = new Dictionary<string, List<object>>
            {
                { "ID", [1, 2, 3] },
                { "Name", ["A", "B", "C"] },
                { "Value", [1.1, 2.2, 3.3] }
            }
        };

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