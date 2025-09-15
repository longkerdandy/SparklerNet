using JetBrains.Annotations;

namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug Template is used for encoding complex datatypes in a payload. It is a type of metric and can be used to
///     create custom datatype definitions and instances. These are also sometimes referred to as User Defined Types or
///     UDTs. There are two types of Templates: Template Definition and Template Instance.
/// </summary>
[PublicAPI]
public record Template
{
    // An optional field representing the version of the Template.
    public string? Version { get; init; }

    // The members of the Template.
    public List<Metric> Metrics { get; init; } = [];

    // An optional field representing parameters associated with the Template.
    public List<Parameter>? Parameters { get; init; }

    // The reference to a Template Definition name if this is a Template Instance.
    public string? TemplateRef { get; init; }

    // This is a Template definition or a Template instance?
    public bool IsDefinition { get; init; }
}