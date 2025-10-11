using MQTTnet;
using MQTTnet.Packets;
using SparklerNet.Core.Extensions;
using Xunit;

namespace SparklerNet.Tests.Core.Extensions;

public class MqttClientSubscribeResultExtensionsTests
{
    [Fact]
    public void ToFormattedString_NullParameter_ThrowsArgumentNullException()
    {
        MqttClientSubscribeResult? nullResult = null;
        Assert.Throws<ArgumentNullException>(() => nullResult!.ToFormattedString());
    }

    [Fact]
    public void ToFormattedString_MultipleSubscriptions_ReturnsCorrectlyFormattedString()
    {
        var topicFilter1 = new MqttTopicFilter { Topic = "test/topic1" };
        var topicFilter2 = new MqttTopicFilter { Topic = "test/topic2" };
        var resultItem1 = new MqttClientSubscribeResultItem(topicFilter1, MqttClientSubscribeResultCode.GrantedQoS0);
        var resultItem2 = new MqttClientSubscribeResultItem(topicFilter2, MqttClientSubscribeResultCode.GrantedQoS1);
        var items = new List<MqttClientSubscribeResultItem> { resultItem1, resultItem2 };
        var subscribeResult = new MqttClientSubscribeResult(1, items, null, new List<MqttUserProperty>());
        var result = subscribeResult.ToFormattedString();
        Assert.Equal("test/topic1 [GrantedQoS0], test/topic2 [GrantedQoS1]", result);
    }

    [Fact]
    public void ToFormattedString_EmptyItems_ReturnsEmptyString()
    {
        var subscribeResult = new MqttClientSubscribeResult(1, new List<MqttClientSubscribeResultItem>(), null,
            new List<MqttUserProperty>());
        var result = subscribeResult.ToFormattedString();
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToFormattedString_SingleItem_ReturnsCorrectFormat()
    {
        var topicFilter = new MqttTopicFilter { Topic = "test/topic1" };
        var resultItem = new MqttClientSubscribeResultItem(topicFilter, MqttClientSubscribeResultCode.GrantedQoS0);
        var items = new List<MqttClientSubscribeResultItem> { resultItem };
        var subscribeResult = new MqttClientSubscribeResult(1, items, null, new List<MqttUserProperty>());
        var result = subscribeResult.ToFormattedString();
        Assert.Equal("test/topic1 [GrantedQoS0]", result);
    }

    [Fact]
    public void ToFormattedString_SpecialCharactersInTopic_ReturnsCorrectFormat()
    {
        var topicFilter = new MqttTopicFilter { Topic = "special/topic/with/numbers/123/and/symbols/+/#" };
        var resultItem = new MqttClientSubscribeResultItem(topicFilter, MqttClientSubscribeResultCode.GrantedQoS0);
        var items = new List<MqttClientSubscribeResultItem> { resultItem };
        var subscribeResult = new MqttClientSubscribeResult(1, items, null, new List<MqttUserProperty>());
        var result = subscribeResult.ToFormattedString();
        Assert.Equal("special/topic/with/numbers/123/and/symbols/+/# [GrantedQoS0]", result);
    }

    [Fact]
    public void ToFormattedString_DifferentResultCodes_ReturnsCorrectFormat()
    {
        var topicFilter1 = new MqttTopicFilter { Topic = "test/topic1" };
        var topicFilter2 = new MqttTopicFilter { Topic = "test/topic2" };
        var resultItem1 = new MqttClientSubscribeResultItem(topicFilter1, MqttClientSubscribeResultCode.GrantedQoS0);
        var resultItem2 =
            new MqttClientSubscribeResultItem(topicFilter2, MqttClientSubscribeResultCode.UnspecifiedError);
        var items = new List<MqttClientSubscribeResultItem> { resultItem1, resultItem2 };
        var subscribeResult = new MqttClientSubscribeResult(1, items, null, new List<MqttUserProperty>());
        var result = subscribeResult.ToFormattedString();
        Assert.Equal("test/topic1 [GrantedQoS0], test/topic2 [UnspecifiedError]", result);
    }
}