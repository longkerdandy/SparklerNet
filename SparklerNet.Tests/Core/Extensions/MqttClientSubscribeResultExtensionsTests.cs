using MQTTnet;
using MQTTnet.Packets;
using SparklerNet.Core.Extensions;
using Xunit;
using static MQTTnet.MqttClientSubscribeResultCode;

namespace SparklerNet.Tests.Core.Extensions;

public class MqttClientSubscribeResultExtensionsTests
{
    [Fact]
    public void ToFormattedString_NullParameter_ThrowsArgumentNullException()
    {
        MqttClientSubscribeResult? nullResult = null;
        Assert.Throws<ArgumentNullException>(() => nullResult!.ToFormattedString());
    }

    [Theory]
    [InlineData(new object[] { }, "")]
    [InlineData(new object[] { "test/topic1", GrantedQoS0 }, "test/topic1 [GrantedQoS0]")]
    [InlineData(new object[] { "test/topic1", GrantedQoS0, "test/topic2", GrantedQoS1 },
        "test/topic1 [GrantedQoS0], test/topic2 [GrantedQoS1]")]
    [InlineData(new object[] { "special/topic/with/numbers/123/and/symbols/+/#", GrantedQoS0 },
        "special/topic/with/numbers/123/and/symbols/+/# [GrantedQoS0]")]
    [InlineData(new object[] { "test/topic1", GrantedQoS0, "test/topic2", UnspecifiedError },
        "test/topic1 [GrantedQoS0], test/topic2 [UnspecifiedError]")]
    public void ToFormattedString_WithTopicAndResultCodeCombinations_ReturnsExpectedFormat(object[] parameters,
        string expectedResult)
    {
        var items = new List<MqttClientSubscribeResultItem>();

        for (var i = 0; i < parameters.Length; i += 2)
        {
            var topic = (string)parameters[i];
            var resultCode = (MqttClientSubscribeResultCode)parameters[i + 1];
            var topicFilter = new MqttTopicFilter { Topic = topic };
            var resultItem = new MqttClientSubscribeResultItem(topicFilter, resultCode);
            items.Add(resultItem);
        }

        var subscribeResult = new MqttClientSubscribeResult(1, items, null, new List<MqttUserProperty>());
        var result = subscribeResult.ToFormattedString();

        Assert.Equal(expectedResult, result);
    }
}