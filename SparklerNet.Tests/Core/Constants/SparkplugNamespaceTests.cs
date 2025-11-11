using SparklerNet.Core.Constants;
using Xunit;

namespace SparklerNet.Tests.Core.Constants;

public class SparkplugNamespaceTests
{
    [Theory]
    [InlineData(SparkplugVersion.V300, "spBv1.0")]
    public void FromSparkplugVersion_ReturnsCorrectNamespace(SparkplugVersion version, string expectedNamespace)
    {
        var result = SparkplugNamespace.FromSparkplugVersion(version);
        Assert.Equal(expectedNamespace, result);
    }

    [Fact]
    public void FromSparkplugVersion_UnsupportedVersion_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => SparkplugNamespace.FromSparkplugVersion((SparkplugVersion)999));
    }

    [Theory]
    [InlineData("spBv1.0", SparkplugVersion.V300)]
    [InlineData("SPBV1.0", SparkplugVersion.V300)] // Case-insensitive test
    public void ToSparkplugVersion_ReturnsCorrectVersion(string @namespace, SparkplugVersion expectedVersion)
    {
        var result = SparkplugNamespace.ToSparkplugVersion(@namespace);
        Assert.Equal(expectedVersion, result);
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
            // ReSharper disable once - AssignNullToNotNullAttribute
            // ReSharper disable once - ReturnValueOfPureMethodIsNotUsed
            SparkplugNamespace.NamespaceElementRegex().IsMatch(null!);
        });
        // Verify the exception message contains information about the null parameter
        Assert.Equal("input", exception.ParamName);
    }

    [Theory]
    [InlineData("")] // Empty string
    [InlineData(" ")] // Space
    [InlineData("  ")] // Multiple spaces
    [InlineData("\t")] // Tab
    [InlineData("\n")] // Newline
    [InlineData(" \t \n ")] // Mixed whitespace
    [InlineData("test+string")] // Contains '+'
    [InlineData("+only")] // Starts with '+'
    [InlineData("string/with/slashes")] // Contains '/'
    [InlineData("#hash")] // Contains '#'
    [InlineData("+/#all")] // Contains all reserved chars
    public void NamespaceElementRegex_InvalidOrReservedStrings_Matches(string input)
    {
        var result = SparkplugNamespace.NamespaceElementRegex().IsMatch(input);
        Assert.True(result, $"String '{input}' should match but didn't");
    }

    [Theory]
    [InlineData("validstring")] // Simple alphanumeric
    [InlineData("valid-string")] // With hyphen
    [InlineData("valid.string")] // With dot
    [InlineData("valid_string123")] // With underscore and numbers
    public void NamespaceElementRegex_ValidStrings_DoesNotMatch(string input)
    {
        var result = SparkplugNamespace.NamespaceElementRegex().IsMatch(input);
        Assert.False(result, $"String '{input}' should not match but did");
    }
}