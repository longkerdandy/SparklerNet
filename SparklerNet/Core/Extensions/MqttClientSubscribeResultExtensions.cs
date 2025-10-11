using MQTTnet;

namespace SparklerNet.Core.Extensions;

/// <summary>
///     Extension methods for MqttClientSubscribeResult
/// </summary>
public static class MqttClientSubscribeResultExtensions
{
    /// <summary>
    ///     Formats the MqttClientSubscribeResult into a string with format "Topic [ResultCode]".
    /// </summary>
    /// <param name="result">The subscribe result</param>
    /// <returns>Formatted string representing all subscription results</returns>
    public static string ToFormattedString(this MqttClientSubscribeResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        // Format each subscription result item
        var formattedItems = result.Items.Select(item => $"{item.TopicFilter.Topic} [{item.ResultCode}]").ToList();
        return string.Join(", ", formattedItems);
    }
}