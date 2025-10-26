// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug DataSet object is used to encode matrices of data.
/// </summary>
public record DataSet
{
    /// <summary>
    ///     Column names.
    /// </summary>
    public List<string> Columns { get; init; } = [];

    /// <summary>
    ///     Column data types MUST be one of the enumerated values as shown in the Sparkplug Basic Data Types.
    /// </summary>
    public List<DataType> Types { get; init; } = [];

    /// <summary>
    ///     Column data. Key: Column name, Value: Column of data.
    /// </summary>
    public Dictionary<string, List<object>> ColumnData { get; set; } = [];

    /// <summary>
    ///     Number of rows.
    /// </summary>
    public int RowCount => Columns.Count > 0 && ColumnData.TryGetValue(Columns[0], out var data) ? data.Count : 0;

    /// <summary>
    ///     Number of columns.
    /// </summary>
    public int ColumnCount => Columns.Count;

    /// <summary>
    ///     Adds a new row of data to the DataSet.
    /// </summary>
    /// <param name="rowData">The data values for the new row, in column order.</param>
    /// <exception cref="InvalidOperationException">Thrown when the number of values does not match the number of columns.</exception>
    public void AddRow(List<object> rowData)
    {
        if (rowData.Count != Columns.Count)
            throw new InvalidOperationException(
                $"The number of values ({rowData.Count}) does not match the number of columns ({Columns.Count}).");

        for (var i = 0; i < Columns.Count; i++)
        {
            var columnName = Columns[i];
            if (!ColumnData.ContainsKey(columnName)) ColumnData[columnName] = [];
            ColumnData[columnName].Add(rowData[i]);
        }
    }

    /// <summary>
    ///     Gets all rows of data in the DataSet.
    /// </summary>
    /// <returns>Enumerable of rows, where each row is a list of values in column order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a column does not have enough values for a row.</exception>
    public IEnumerable<List<object>> GetRows()
    {
        var rowCount = RowCount;
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var rowData = new List<object>(Columns.Count);
            foreach (var columnName in Columns)
                if (ColumnData.TryGetValue(columnName, out var columnValues) && rowIndex < columnValues.Count)
                    rowData.Add(columnValues[rowIndex]);
                else
                    throw new InvalidOperationException(
                        $"DataSet column '{columnName}' does not have enough values for row {rowIndex}.");
            yield return rowData;
        }
    }
}