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

    [Fact]
    public void NamespaceElementRegex_NullInput_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            // This is an explicit test for null handling
            // ReSharper disable once AssignNullToNotNullAttribute
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            SparkplugNamespace.NamespaceElementRegex().IsMatch(null!);
        });
        // Verify the exception message contains information about the null parameter
        Assert.Equal("input", exception.ParamName);
    }

    [Fact]
    public void NamespaceElementRegex_EmptyString_Matches()
    {
        var result = SparkplugNamespace.NamespaceElementRegex().IsMatch(string.Empty);
        Assert.True(result);
    }

    [Fact]
    public void NamespaceElementRegex_WhitespaceOnlyString_Matches()
    {
        string[] whitespaceStrings = [" ", "  ", "\t", "\n", " \t \n "];
        foreach (var testString in whitespaceStrings)
            Assert.True(SparkplugNamespace.NamespaceElementRegex().IsMatch(testString),
                $"String '{testString}' should match but didn't");
    }

    [Fact]
    public void NamespaceElementRegex_ContainsReservedCharacters_Matches()
    {
        string[] stringsWithReservedChars = ["test+string", "+only", "string/with/slashes", "#hash", "+/#all"];
        foreach (var testString in stringsWithReservedChars)
            Assert.True(SparkplugNamespace.NamespaceElementRegex().IsMatch(testString),
                $"String '{testString}' should match but didn't");
    }

    [Fact]
    public void NamespaceElementRegex_ValidString_DoesNotMatch()
    {
        string[] validStrings = ["validstring", "valid-string", "valid.string", "valid_string123"];
        foreach (var testString in validStrings)
            Assert.False(SparkplugNamespace.NamespaceElementRegex().IsMatch(testString),
                $"String '{testString}' should not match but did");
    }
}