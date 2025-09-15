using JetBrains.Annotations;

namespace SparklerNet.Core.Model;

/// <summary>
///     The value of a DataSet.DataSetValue.
///     MUST be one of the following Protobuf types: uint32, uint64, float, double, bool, or string.
/// </summary>
[PublicAPI]
public record DataSetValue
{
    // The DataSet value.
    public object? Value { get; init; }
}