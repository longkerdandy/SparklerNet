namespace SparklerNet.Core.Extensions;

/// <summary>
///     Provides extension methods for dictionary operations
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    ///     Updates or adds a value to the dictionary and returns whether the key existed.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The sorted dictionary to update.</param>
    /// <param name="key">The key to update or add.</param>
    /// <param name="newValue">The new value to set.</param>
    /// <param name="oldValue">
    ///     When this method returns, contains the previous value associated with the key, if found;
    ///     otherwise, the default value for the type.
    /// </param>
    /// <returns>true if the key existed and was replaced; false if the key was added.</returns>
    public static bool TryReplace<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey key,
        TValue newValue, out TValue? oldValue) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(key);

        var isExisted = dictionary.TryGetValue(key, out oldValue);
        dictionary[key] = newValue;
        return isExisted;
    }
}