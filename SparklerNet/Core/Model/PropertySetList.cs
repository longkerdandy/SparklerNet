// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug PropertySetList object is an array of <see cref="PropertySet" /> objects.
/// </summary>
public record PropertySetList
{
    /// <summary>
    ///     A list of <see cref="PropertySet" /> objects.
    /// </summary>
    public List<PropertySet> PropertySets { get; init; } = [];
}