using JetBrains.Annotations;

namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug metric is a core component of data in the payload. It represents a key, value, timestamp, and datatype
///     along with metadata used to describe the information it contains. These also represent tags in classic SCADA
///     systems.
/// </summary>
[PublicAPI]
public record Metric
{
    // The friendly name of the metric.
    public string? Name { get; init; }

    // Representing an optional alias.
    public ulong? Alias { get; init; }

    // Milliseconds since epoch.
    public long? Timestamp { get; init; }

    // Sparkplug date type.
    public DataType? DateType { get; init; }

    // The metric represents a historical value?
    public bool? IsHistorical { get; init; }

    // The metric should be considered as transient?
    public bool? IsTransient { get; init; }

    // The metric value is null?
    public bool? IsNull { get; init; }

    // MetaData object associated with the metric.
    public MetaData? Metadata { get; init; }

    // PropertySet object associated with the metric.
    public PropertySet? Properties { get; init; }

    // The metric value.
    public object? Value { get; init; }
}