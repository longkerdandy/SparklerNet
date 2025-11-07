using System.Dynamic;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Events;
using SparklerNet.Core.Model;
using SparklerNet.Core.Options;
using SparklerNet.HostApplication.Caches;
using Xunit;

namespace SparklerNet.Tests.HostApplication.Caches;

public class MessageOrderingServiceTests
{
    private readonly MessageOrderingService _service;

    public MessageOrderingServiceTests()
    {
        var options = new SparkplugClientOptions
        {
            HostApplicationId = "TestHost",
            SeqReorderTimeout = 1000,
            ProcessDisorderedMessages = true,
            SendRebirthWhenTimeout = true
        };
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        _service = new MessageOrderingService(cache, options);
    }

    [Theory]
    [InlineData("Group1", "Edge1", null, null, "Group1:Edge1")]
    [InlineData("Group1", "Edge1", "Device1", null, "Group1:Edge1:Device1")]
    [InlineData("Group1", "Edge1", null, "Prefix_", "Prefix_Group1:Edge1")]
    [InlineData("Group1", "Edge1", "Device1", "Prefix_", "Prefix_Group1:Edge1:Device1")]
    [InlineData("Group1", "Edge1", "", "Prefix_", "Prefix_Group1:Edge1")]
    [InlineData("Group1", "Edge1", "Device1", "", "Group1:Edge1:Device1")]
    public void BuildCacheKey_ShouldReturnCorrectKey(string groupId, string edgeNodeId, string? deviceId,
        string? prefix, string expectedKey)
    {
        var result = MessageOrderingService.BuildCacheKey(prefix, groupId, edgeNodeId, deviceId);
        Assert.Equal(expectedKey, result);
    }

    [Theory]
    // Normal integer comparison cases
    [InlineData(10, 20, -1)]
    [InlineData(20, 10, 1)]
    [InlineData(15, 15, 0)]
    // Wrap-around cases: x near 0 and y near 255 (x should be considered larger)
    [InlineData(10, 240, 1)]
    [InlineData(20, 230, 1)]
    [InlineData(31, 224, 1)]
    // Wrap-around cases: x near 255 and y near 0 (x should be considered smaller)
    [InlineData(240, 10, -1)]
    [InlineData(230, 20, -1)]
    [InlineData(224, 31, -1)]
    // Boundary cases testing
    [InlineData(32, 223, -1)] // Threshold boundary, should use normal comparison
    [InlineData(223, 32, 1)] // Threshold boundary, should use normal comparison
    public void CircularSequenceComparer_ShouldCompareCorrectly(int x, int y, int expectedResult)
    {
        var comparer = new MessageOrderingService.CircularSequenceComparer();
        var result = comparer.Compare(x, y);
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void ProcessMessageOrder_ShouldProcessContinuousSequence()
    {
        var context1 = CreateMessageContext(1);
        var context2 = CreateMessageContext(2);
        var context3 = CreateMessageContext(3);

        var result1 = _service.ProcessMessageOrder(context1);
        var result2 = _service.ProcessMessageOrder(context2);
        var result3 = _service.ProcessMessageOrder(context3);

        Assert.Single(result1);
        Assert.Single(result2);
        Assert.Single(result3);
        // We can't directly access the Seq property due to reflection, but we can verify message count and processing
    }

    [Fact]
    public void ProcessMessageOrder_ShouldCacheOutOfOrderMessages()
    {
        var context1 = CreateMessageContext(1);
        var context3 = CreateMessageContext(3);
        var context2 = CreateMessageContext(2);

        var result1 = _service.ProcessMessageOrder(context1);
        var result3 = _service.ProcessMessageOrder(context3);
        var result2 = _service.ProcessMessageOrder(context2);

        Assert.Single(result1);
        Assert.Empty(result3); // Should be cached
        Assert.Equal(2, result2.Count); // Should process both messages 2 and 3
        Assert.Equal(2, result2[0].Payload.Seq);
        Assert.Equal(3, result2[1].Payload.Seq);
    }

    [Fact]
    public void ProcessMessageOrder_ShouldHandleSequenceWrapAround()
    {
        var context254 = CreateMessageContext(254);
        var context255 = CreateMessageContext(255);
        var context0 = CreateMessageContext(0);
        var context1 = CreateMessageContext(1);

        var result254 = _service.ProcessMessageOrder(context254);
        var result255 = _service.ProcessMessageOrder(context255);
        var result0 = _service.ProcessMessageOrder(context0);
        var result1 = _service.ProcessMessageOrder(context1);

        Assert.Single(result254);
        Assert.Single(result255);
        Assert.Single(result0); // Should handle wrap-around
        Assert.Single(result1);
    }

    [Fact]
    public void ProcessMessageOrder_ShouldRejectInvalidSequenceNumbers()
    {
        var invalidContextNegative = CreateMessageContext(-1);
        var invalidContextTooHigh = CreateMessageContext(256);

        var resultNegative = _service.ProcessMessageOrder(invalidContextNegative);
        var resultTooHigh = _service.ProcessMessageOrder(invalidContextTooHigh);

        Assert.Empty(resultNegative);
        Assert.Empty(resultTooHigh);
    }

    [Fact]
    public void ClearMessageOrderCache_ShouldRemoveCachedItems()
    {
        var context1 = CreateMessageContext(1);
        var context3 = CreateMessageContext(3); // Will be cached
        _service.ProcessMessageOrder(context1);
        _service.ProcessMessageOrder(context3);

        _service.ClearMessageOrderCache("Group1", "Edge1", "Device1");
        var context2 = CreateMessageContext(2); // Should not process context3 now
        var result2 = _service.ProcessMessageOrder(context2);

        Assert.Single(result2); // Only context2 is processed, context3 was cleared from cache
        Assert.Equal(2, result2[0].Payload.Seq);
    }

    [Fact]
    public async Task OnReorderTimeout_ShouldProcessPendingMessages_WhenProcessDisorderedMessagesEnabled()
    {
        List<SparkplugMessageEventArgs>? processedMessages = null;
        _service.OnPendingMessages = messages =>
        {
            processedMessages = [.. messages];
            return Task.CompletedTask;
        };

        var context1 = CreateMessageContext(1);
        var context3 = CreateMessageContext(3);
        _service.ProcessMessageOrder(context1);
        _service.ProcessMessageOrder(context3);

        const string timerKey = "Group1:Edge1:Device1";
        _service.GetType().GetMethod("OnReorderTimeout", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(_service, [timerKey]);

        // Allow async operation to complete
        await Task.Delay(100);

        Assert.NotNull(processedMessages);
        Assert.Single(processedMessages);
        Assert.Equal(3, processedMessages[0].Payload.Seq);
    }

    [Fact]
    public async Task OnReorderTimeout_ShouldSendRebirthRequest_WhenSendRebirthWhenTimeoutEnabled()
    {
        string? actualGroupId = null;
        string? actualEdgeNodeId = null;
        string? actualDeviceId = null;
        _service.OnRebirthRequested = (groupId, edgeNodeId, deviceId) =>
        {
            actualGroupId = groupId;
            actualEdgeNodeId = edgeNodeId;
            actualDeviceId = deviceId;
            return Task.CompletedTask;
        };

        const string timerKey = "Group1:Edge1:Device1";
        _service.GetType().GetMethod("OnReorderTimeout", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(_service, [timerKey]);

        // Allow async operation to complete
        await Task.Delay(100);

        Assert.Equal("Group1", actualGroupId);
        Assert.Equal("Edge1", actualEdgeNodeId);
        Assert.Equal("Device1", actualDeviceId);
    }

    private static SparkplugMessageEventArgs CreateMessageContext(int sequenceNumber)
    {
        // Create payload with specified sequence number
        var payload = new Payload();
        var seqProperty = payload.GetType().GetProperty("Seq");
        seqProperty?.SetValue(payload, sequenceNumber);

        // Create MessageContext using reflection to bypass constructor requirements
        // Reflection is needed because MqttApplicationMessageReceivedEventArgs is difficult to create in tests
        var messageContext = (SparkplugMessageEventArgs)Activator.CreateInstance(typeof(SparkplugMessageEventArgs), true)!;

        // Set required properties

        SetPrivateProperty(messageContext, "GroupId", "Group1");
        SetPrivateProperty(messageContext, "EdgeNodeId", "Edge1");
        SetPrivateProperty(messageContext, "DeviceId", "Device1");
        SetPrivateProperty(messageContext, "Payload", payload);
        SetPrivateProperty(messageContext, "Version", SparkplugVersion.V300);
        SetPrivateProperty(messageContext, "MessageType", SparkplugMessageType.NDATA);

        // Create dynamic object for EventArgs
        dynamic eventArgs = new ExpandoObject();
        SetPrivateField(messageContext, "EventArgs", eventArgs);

        return messageContext;
    }

    // Helper method to set private properties using reflection
    private static void SetPrivateProperty(object obj, string propertyName, object value)
    {
        var property = obj.GetType().GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null) property.SetValue(obj, value);
        else SetPrivateField(obj, propertyName, value);
    }

    // Helper method to set private fields using reflection
    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType()
            .GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(obj, value);
    }
}