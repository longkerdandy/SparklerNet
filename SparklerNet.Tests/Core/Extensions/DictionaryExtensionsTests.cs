using SparklerNet.Core.Extensions;
using Xunit;

namespace SparklerNet.Tests.Core.Extensions;

public class DictionaryExtensionsTests
{
    [Theory]
    [InlineData(new[] { "key1", "value1" }, "key2", "newValue", "", false)] // Add a new key
    [InlineData(new[] { "key1", "value1" }, "key1", "newValue", "value1", true)] // Replace the existing key
    [InlineData(new object[] { }, "key1", "value1", "", false)] // Empty dictionary
    public void TryReplace_WithDifferentScenarios_ReturnsExpectedResult(
        object[] initialData,
        string key,
        string newValue,
        string expectedOldValue,
        bool expectedResult)
    {
        var dictionary = new SortedDictionary<string, string>();

        // Populate initial data
        for (var i = 0; i < initialData.Length; i += 2)
            if (initialData[i] is string dictKey && initialData[i + 1] is string dictValue)
                dictionary[dictKey] = dictValue;

        var result = dictionary.TryReplace(key, newValue, out var oldValue);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedOldValue, oldValue ?? string.Empty);
        Assert.True(dictionary.ContainsKey(key));
        Assert.Equal(newValue, dictionary[key]);
    }

    [Fact]
    public void TryReplace_WithNullDictionary_ThrowsArgumentNullException()
    {
        SortedDictionary<string, string>? dictionary = null;
        Assert.Throws<ArgumentNullException>(() => dictionary!.TryReplace("key", "value", out _));
    }

    [Fact]
    public void TryReplace_WithNullKey_ThrowsArgumentNullException()
    {
        var dictionary = new SortedDictionary<string, string>();
        Assert.Throws<ArgumentNullException>(() => dictionary.TryReplace(null!, "value", out _));
    }

    [Fact]
    public void TryReplace_WithIntegerValues_WorksCorrectly()
    {
        var dictionary = new SortedDictionary<int, int> { { 1, 42 } };

        var result = dictionary.TryReplace(1, 100, out var oldValue);

        Assert.True(result);
        Assert.Equal(42, oldValue);
        Assert.Equal(100, dictionary[1]);
    }

    [Theory]
    [InlineData("oldValue", "newValue", true)]
    [InlineData(null, "newValue", false)]
    [InlineData("oldValue", null, true)]
    public void TryReplace_WithStringValues_WorksCorrectly(string? initialValue, string? newValue, bool expectedResult)
    {
        var dictionary = new SortedDictionary<int, string?>();
        const int key = 1;

        if (initialValue != null) dictionary[key] = initialValue;

        var result = dictionary.TryReplace(key, newValue, out var oldValue);

        Assert.Equal(expectedResult, result);
        Assert.Equal(initialValue, oldValue);
        Assert.True(dictionary.ContainsKey(key));
        Assert.Equal(newValue, dictionary[key]);
    }
}