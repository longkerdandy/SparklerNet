using JetBrains.Annotations;

namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug Template Parameter is a metadata field for a Template.
///     MUST be one of the enumerated values as shown in the Sparkplug Basic Data Types.
/// </summary>
[PublicAPI]
public record Parameter
{
    // The name of the template parameter.
    public string? Name { get; init; }

    // The template parameter date type.
    public DataType Type { get; init; }

    // The template parameter value.
    public object? Value { get; init; }
}