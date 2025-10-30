// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug PropertyValue object is used to encode the value and datatype of a property in a PropertySet.
/// </summary>
public record PropertyValue
{
    /// <summary>
    ///     The data type of the property value.
    ///     MUST be one of the enumerated values as shown in Basic Data Types or Property Value Data Types.
    /// </summary>
    public DataType Type { get; init; }

    /// <summary>
    ///     Indicates whether the property value is null.
    /// </summary>
    public bool IsNull => Value is null;

    /// <summary>
    ///     The property value.
    /// </summary>
    public object? Value { get; set; }
}