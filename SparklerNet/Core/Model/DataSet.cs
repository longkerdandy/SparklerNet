using JetBrains.Annotations;

namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug DataSet object is used to encode matrices of data.
///     The types array MUST be one of the enumerated values as shown in the Sparkplug Basic Data Types.
/// </summary>
[PublicAPI]
public record DataSet
{
    // The number of columns in this DataSet.
    public ulong NumberOfColumns { get; init; }

    // The column headers of this DataSet.
    public List<string> Columns { get; init; } = [];

    // The column types of this DataSet.
    public List<DataType> Types { get; init; } = [];

    // The array of DataSetRow objects.
    public List<DataSetRow> Rows { get; init; } = [];
}