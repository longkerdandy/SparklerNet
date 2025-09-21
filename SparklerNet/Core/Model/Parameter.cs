using JetBrains.Annotations;

namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug Template Parameter is a metadata field for a Template.
///     MUST be one of the enumerated values as shown in the Sparkplug Basic Data Types.
/// </summary>
[PublicAPI]
public record Parameter
{
    /// <summary>
    ///     The name of the template parameter.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     The template parameter date type, MUST be one of the enumerated values as shown in the Sparkplug Basic Data Types.
    /// </summary>
    public DataType Type { get; init; }

    /// <summary>
    ///     The template parameter value.
    /// </summary>
    public object? Value { get; set; }
}