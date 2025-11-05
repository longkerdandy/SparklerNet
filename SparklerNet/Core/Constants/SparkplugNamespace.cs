using System.Text.RegularExpressions;

namespace SparklerNet.Core.Constants;

/// <summary>
///     The namespace element of the topic namespace is the root element that will define both the structure of the
///     remaining namespace elements and the encoding used for the associated payload data. The Sparkplug
///     specification defines two namespaces. One is for Sparkplug payload definition A (now deprecated), and the
///     second is for the Sparkplug payload definition B.
///     Only Sparkplug payload definition B is supported in SparklerNet.
/// </summary>
public static partial class SparkplugNamespace
{
    private const string SparkplugBv1 = "spBv1.0";

    // Regular expression to match reserved characters in namespace elements (+, /, #) and empty strings
    [GeneratedRegex(@"^\s*$|[+/#]", RegexOptions.Compiled)]
    public static partial Regex NamespaceElementRegex();

    /// <summary>
    ///     Validates a namespace element to ensure it does not contain reserved characters (+, /, #) or is empty/whitespace.
    /// </summary>
    /// <param name="element">The namespace element to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentNullException">Thrown when element is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when element is invalid (empty, whitespace only, or contains reserved
    ///     characters).
    /// </exception>
    public static void ValidateNamespaceElement(string element, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(element, parameterName);

        if (NamespaceElementRegex().IsMatch(element))
            throw new ArgumentException(
                $"{parameterName} cannot be empty or contain reserved characters +, / or #.", parameterName);
    }

    /// <summary>
    ///     Convert a <see cref="SparkplugVersion" /> to <see cref="SparkplugNamespace" />.
    /// </summary>
    /// <param name="version">Sparkplug version</param>
    /// <returns>Sparkplug namespace</returns>
    /// <exception cref="NotSupportedException">Thrown when the Sparkplug version is not supported.</exception>
    public static string FromSparkplugVersion(SparkplugVersion version)
    {
        return version switch
        {
            SparkplugVersion.V300 => SparkplugBv1,
            _ => throw new NotSupportedException($"Not supported Sparkplug version {version}.")
        };
    }

    /// <summary>
    ///     Convert a <see cref="SparkplugNamespace" /> to <see cref="SparkplugVersion" />.
    /// </summary>
    /// <param name="namespace">Sparkplug namespace</param>
    /// <returns>Sparkplug version</returns>
    /// <exception cref="NotSupportedException">Thrown when the Sparkplug namespace is not supported.</exception>
    public static SparkplugVersion ToSparkplugVersion(string @namespace)
    {
        return string.Equals(SparkplugBv1, @namespace, StringComparison.OrdinalIgnoreCase)
            ? SparkplugVersion.V300
            : throw new NotSupportedException($"Not supported Sparkplug namespace {@namespace}.");
    }
}