namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug Template is used for encoding complex datatypes in a payload. It is a type of metric and can be used to
///     create custom datatype definitions and instances. These are also sometimes referred to as User Defined Types or
///     UDTs. There are two types of Templates: Template Definition and Template Instance.
/// </summary>
public record Template
{
    /// <summary>
    ///     The version of the Template.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    ///     The members of the Template.
    /// </summary>
    public List<Metric> Metrics { get; set; } = [];

    /// <summary>
    ///     The parameters associated with the Template.
    /// </summary>
    public List<Parameter>? Parameters { get; set; }

    /// <summary>
    ///     The reference to a Template Definition name if this is a Template Instance.
    /// </summary>
    public string? TemplateRef { get; init; }

    /// <summary>
    ///     Indicates whether the Template is a Definition or an Instance.
    /// </summary>
    public bool IsDefinition { get; init; }
}