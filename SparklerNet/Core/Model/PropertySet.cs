namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug PropertySet object is used with a metric to add custom properties to the object. The PropertySet is a
///     map expressed as two arrays of equal size, one containing the keys and one containing the
///     <see cref="PropertyValue" /> objects.
/// </summary>
public record PropertySet
{
    /// <summary>
    ///     A dictionary of key-value pairs, where the key is a string and the value is a <see cref="PropertyValue" />.
    /// </summary>
    public Dictionary<string, PropertyValue> Properties { get; init; } = [];
}