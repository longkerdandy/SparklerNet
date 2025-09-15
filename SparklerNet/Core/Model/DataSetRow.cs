using JetBrains.Annotations;

namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug DataSet.Row object represents a row of data in a DataSet.
/// </summary>
[PublicAPI]
public record DataSetRow
{
    // The data contained within a row of a DataSet.
    public List<DataSetValue> Elements { get; init; } = [];
}