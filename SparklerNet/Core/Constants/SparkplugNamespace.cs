namespace SparklerNet.Core.Constants;

/// <summary>
///     The namespace element of the topic namespace is the root element that will define both the structure of the
///     remaining namespace elements as well as the encoding used for the associated payload data. The Sparkplug
///     specification defines two namespaces. One is for Sparkplug payload definition A (now deprecated), and the
///     second is for the Sparkplug payload definition B.
///     Only Sparkplug payload definition B is supported in SparklerNet.
/// </summary>
public static class SparkplugNamespace
{
    private const string SparkplugBv1 = "spBv1.0";

    /// <summary>
    ///     Convert a <see cref="SparkplugVersion" /> to <see cref="SparkplugNamespace" />.
    /// </summary>
    /// <param name="version">Sparkplug version</param>
    /// <returns>Sparkplug namespace</returns>
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
    public static SparkplugVersion ToSparkplugVersion(string @namespace)
    {
        if (string.Equals(SparkplugBv1, @namespace, StringComparison.OrdinalIgnoreCase)) return SparkplugVersion.V300;
        throw new NotSupportedException($"Not supported Sparkplug namespace {@namespace}.");
    }
}