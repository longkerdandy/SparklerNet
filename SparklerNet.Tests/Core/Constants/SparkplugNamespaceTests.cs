using SparklerNet.Core.Constants;
using Xunit;

namespace SparklerNet.Tests.Core.Constants;

public class SparkplugNamespaceTests
{
    [Fact]
    public void FromSparkplugVersion_V300_ReturnsCorrectNamespace()
    {
        var result = SparkplugNamespace.FromSparkplugVersion(SparkplugVersion.V300);
        Assert.Equal("spBv1.0", result);
    }

    [Fact]
    public void FromSparkplugVersion_UnsupportedVersion_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => SparkplugNamespace.FromSparkplugVersion((SparkplugVersion)999));
    }

    [Fact]
    public void ToSparkplugVersion_SpBv1_0_ReturnsV300()
    {
        var result = SparkplugNamespace.ToSparkplugVersion("spBv1.0");
        Assert.Equal(SparkplugVersion.V300, result);
    }

    [Fact]
    public void ToSparkplugVersion_SpBv1_0_CaseInsensitive()
    {
        var result = SparkplugNamespace.ToSparkplugVersion("SPBV1.0");
        Assert.Equal(SparkplugVersion.V300, result);
    }

    [Fact]
    public void ToSparkplugVersion_UnsupportedNamespace_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            SparkplugNamespace.ToSparkplugVersion("unsupported-namespace"));
    }
}