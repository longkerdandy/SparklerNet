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
    /// <summary>
    ///     The name of the metric.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     The alias of the metric.
    /// </summary>
    public ulong? Alias { get; init; }

    /// <summary>
    ///     The timestamp of the metric value in milliseconds since epoch.
    /// </summary>
    public long? Timestamp { get; init; }

    /// <summary>
    ///     The date type of the metric value.
    /// </summary>
    public DataType? DateType { get; init; }

    /// <summary>
    ///     Indicates whether the metric represents a historical value.
    /// </summary>
    public bool? IsHistorical { get; init; }

    /// <summary>
    ///     Indicates whether the metric should be considered as transient.
    /// </summary>
    public bool? IsTransient { get; init; }

    /// <summary>
    ///     Indicates whether the metric value is null.
    /// </summary>
    public bool IsNull => Value is null;

    /// <summary>
    ///     <see cref="MetaData" /> object associated with the metric.
    /// </summary>
    public MetaData? Metadata { get; set; }

    /// <summary>
    ///     <see cref="PropertySet" /> object associated with the metric.
    /// </summary>
    public PropertySet? Properties { get; set; }

    /// <summary>
    ///     The metric value.
    /// </summary>
    public object? Value { get; set; }
}