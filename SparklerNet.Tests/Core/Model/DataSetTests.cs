using SparklerNet.Core.Model;
using Xunit;

namespace SparklerNet.Tests.Core.Model;

public class DataSetTests
{
    [Fact]
    public void DataSet_Initializes_Properties_Correctly()
    {
        var dataSet = new DataSet();
        Assert.Empty(dataSet.Columns);
        Assert.Empty(dataSet.Types);
        Assert.Empty(dataSet.ColumnData);
        Assert.Equal(0, dataSet.RowCount);
        Assert.Equal(0, dataSet.ColumnCount);
    }

    [Theory]
    [InlineData(new[] { "Column1" }, 2)] // More values than columns
    [InlineData(new[] { "Column1", "Column2" }, 1)] // Fewer values than columns
    public void AddRow_Throws_Exception_When_RowData_Count_Does_Not_Match_Columns_Count(string[] columnsArray,
        int rowDataCount)
    {
        var columns = new List<string>(columnsArray);
        var dataSet = new DataSet { Columns = columns };
        var rowData = Enumerable.Range(1, rowDataCount).Cast<object>().ToList();
        Assert.Throws<InvalidOperationException>(() => dataSet.AddRow(rowData));
    }

    [Theory]
    [InlineData(new[] { "Column1" }, new object[] { 42 }, 1)]
    [InlineData(new[] { "Column1", "Column2" }, new object[] { 1, "Test" }, 2)]
    public void AddRow_Adds_Single_Row_Correctly(string[] columnsArray, object[] rowData, int expectedColumnCount)
    {
        var columns = new List<string>(columnsArray);
        var dataSet = new DataSet { Columns = columns };
        dataSet.AddRow(rowData.ToList());

        Assert.Equal(1, dataSet.RowCount);
        Assert.Equal(expectedColumnCount, dataSet.ColumnCount);

        // Verify data in each column
        for (var i = 0; i < columns.Count; i++) Assert.Contains(rowData[i], dataSet.ColumnData[columns[i]]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void AddRow_Adds_Multiple_Rows_Correctly(int rowCount)
    {
        var dataSet = new DataSet
        {
            Columns = ["ID", "Name"],
            Types = [DataType.Int32, DataType.String]
        };

        // Add rows
        for (var i = 1; i <= rowCount; i++) dataSet.AddRow([i, $"Name{i}"]);

        Assert.Equal(rowCount, dataSet.RowCount);
        Assert.Equal(Enumerable.Range(1, rowCount), dataSet.ColumnData["ID"].Cast<int>());
        Assert.Equal(Enumerable.Range(1, rowCount).Select(i => $"Name{i}"), dataSet.ColumnData["Name"].Cast<string>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void GetRows_Returns_All_Rows_Correctly(int rowCount)
    {
        var dataSet = new DataSet
        {
            Columns = ["ID", "Name"],
            Types = [DataType.Int32, DataType.String]
        };

        // Add rows
        for (var i = 1; i <= rowCount; i++) dataSet.AddRow([i, $"Name{i}"]);

        var rows = dataSet.GetRows().ToList();
        Assert.Equal(rowCount, rows.Count);

        // Verify each row
        for (var i = 0; i < rowCount; i++)
        {
            var expectedId = i + 1;
            var expectedName = $"Name{expectedId}";
            Assert.Equal([expectedId, expectedName], rows[i]);
        }
    }

    [Theory]
    [InlineData(true)] // Missing column data
    [InlineData(false)] // Index out of range
    public void GetRows_Throws_Exception_For_InvalidData(bool isMissingColumnData)
    {
        var dataSet = new DataSet
        {
            Columns = ["Column1", "Column2", "Column3"]
        };

        if (isMissingColumnData)
            // Missing Column2
            dataSet.ColumnData = new Dictionary<string, List<object>>
            {
                { "Column1", [1, 2, 3] },
                { "Column3", [true, false, true] }
            };
        else
            // Column2 has fewer values
            dataSet.ColumnData = new Dictionary<string, List<object>>
            {
                { "Column1", [1, 2, 3] },
                { "Column2", ["A"] },
                { "Column3", [true, false, true] }
            };

        Assert.Throws<InvalidOperationException>(() => dataSet.GetRows().ToList());
    }

    [Fact]
    public void GetRows_Handles_Empty_DataSet_Correctly()
    {
        var dataSet = new DataSet();
        var rows = dataSet.GetRows().ToList();
        Assert.Empty(rows);
    }

    [Theory]
    [InlineData(100, 10)] // 100 rows, take first 10
    [InlineData(50, 5)] // 50 rows, take first 5
    public void GetRows_Uses_Yield_Return_For_Lazy_Evaluation(int totalRows, int takeRows)
    {
        var dataSet = new DataSet
        {
            Columns = ["Column1"],
            Types = [DataType.Int32]
        };

        // Add rows
        for (var i = 0; i < totalRows; i++) dataSet.AddRow([i]);

        // Only take the specified number of rows
        var rows = dataSet.GetRows().Take(takeRows).ToList();
        Assert.Equal(takeRows, rows.Count);

        // Verify data
        for (var i = 0; i < takeRows; i++) Assert.Equal([i], rows[i]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void RowCount_Returns_Correct_Value_With_TryGetValue(int expectedRowCount)
    {
        var dataSet = new DataSet
        {
            Columns = ["Column1", "Column2"]
        };

        Assert.Equal(0, dataSet.RowCount);

        // Add data to Column1 with the specified row count
        if (expectedRowCount <= 0) return;
        dataSet.ColumnData["Column1"] = Enumerable.Range(1, expectedRowCount).Cast<object>().ToList();
        Assert.Equal(expectedRowCount, dataSet.RowCount);
    }
}