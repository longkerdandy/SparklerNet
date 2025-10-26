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

    [Fact]
    public void AddRow_Throws_Exception_When_RowData_Count_Does_Not_Match_Columns_Count()
    {
        var dataSet = new DataSet
        {
            Columns = ["Column1", "Column2"]
        };
        var rowData = new List<object> { 1, 2, 3 }; // 3 values but only 2 columns
        Assert.Throws<InvalidOperationException>(() => dataSet.AddRow(rowData));
    }

    [Fact]
    public void AddRow_Adds_Data_Correctly_For_Single_Row()
    {
        var dataSet = new DataSet
        {
            Columns = ["Column1", "Column2", "Column3"],
            Types = [DataType.Int32, DataType.String, DataType.Boolean]
        };
        var rowData = new List<object> { 10, "Test", true };
        dataSet.AddRow(rowData);
        Assert.Equal(1, dataSet.RowCount);
        Assert.Equal(3, dataSet.ColumnCount);
        Assert.Contains(10, dataSet.ColumnData["Column1"]);
        Assert.Contains("Test", dataSet.ColumnData["Column2"]);
        Assert.Contains(true, dataSet.ColumnData["Column3"]);
    }

    [Fact]
    public void AddRow_Adds_Multiple_Rows_Correctly()
    {
        var dataSet = new DataSet
        {
            Columns = ["ID", "Name"],
            Types = [DataType.Int32, DataType.String]
        };
        var row1 = new List<object> { 1, "Alice" };
        var row2 = new List<object> { 2, "Bob" };
        var row3 = new List<object> { 3, "Charlie" };
        dataSet.AddRow(row1);
        dataSet.AddRow(row2);
        dataSet.AddRow(row3);
        Assert.Equal(3, dataSet.RowCount);
        Assert.Equal([1, 2, 3], dataSet.ColumnData["ID"].Cast<int>().ToList());
        Assert.Equal(["Alice", "Bob", "Charlie"], dataSet.ColumnData["Name"].Cast<string>().ToList());
    }

    [Fact]
    public void GetRows_Returns_All_Rows_Correctly()
    {
        var dataSet = new DataSet
        {
            Columns = ["ID", "Name", "Active"],
            Types = [DataType.Int32, DataType.String, DataType.Boolean]
        };
        dataSet.AddRow([1, "Alice", true]);
        dataSet.AddRow([2, "Bob", false]);
        dataSet.AddRow([3, "Charlie", true]);
        var rows = dataSet.GetRows().ToList();
        Assert.Equal(3, rows.Count);
        Assert.Equal([1, "Alice", true], rows[0]);
        Assert.Equal([2, "Bob", false], rows[1]);
        Assert.Equal([3, "Charlie", true], rows[2]);
    }

    [Fact]
    public void GetRows_Throws_Exception_For_Missing_Column_Data()
    {
        var dataSet = new DataSet
        {
            Columns = ["Column1", "Column2", "Column3"],
            ColumnData = new Dictionary<string, List<object>>
            {
                { "Column1", [1, 2, 3] },
                // Column2 is missing
                { "Column3", [true, false, true] }
            }
        };
        Assert.Throws<InvalidOperationException>(() => dataSet.GetRows().ToList());
    }

    [Fact]
    public void GetRows_Throws_Exception_For_Index_Out_Of_Range()
    {
        var dataSet = new DataSet
        {
            Columns = ["Column1", "Column2"],
            ColumnData = new Dictionary<string, List<object>>
            {
                { "Column1", [1, 2] },
                { "Column2", ["A"] } // Only one value for Column2
            }
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

    [Fact]
    public void GetRows_Uses_Yield_Return_For_Lazy_Evaluation()
    {
        var dataSet = new DataSet
        {
            Columns = ["Column1"],
            Types = [DataType.Int32]
        };
        // Add 100 rows to test lazy evaluation
        for (var i = 0; i < 100; i++) dataSet.AddRow([i]);
        // Only take the first 10 rows without listing all
        var rows = dataSet.GetRows().Take(10).ToList();
        Assert.Equal(10, rows.Count);
        for (var i = 0; i < 10; i++) Assert.Equal([i], rows[i]);
    }

    [Fact]
    public void RowCount_Returns_Correct_Value_With_TryGetValue()
    {
        var dataSet = new DataSet
        {
            Columns = ["Column1", "Column2"]
            // ColumnData intentionally aren't initialized with Column1 to test TryGetValue
        };
        Assert.Equal(0, dataSet.RowCount);
        // Add data to Column1
        dataSet.ColumnData["Column1"] = [1, 2, 3];
        Assert.Equal(3, dataSet.RowCount);
    }
}