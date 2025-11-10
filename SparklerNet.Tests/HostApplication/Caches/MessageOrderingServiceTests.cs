using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
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
            SendRebirthWhenTimeout = true
        };
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

        // Setup mock ILoggerFactory
        var mockLogger = new Mock<ILogger<MessageOrderingService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(factory => factory.CreateLogger(It.IsAny<string>()))
            .Returns(mockLogger.Object);

        _service = new MessageOrderingService(cache, options, mockLoggerFactory.Object);
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
    [InlineData(32, 223, -1)] // Threshold boundary should use normal comparison
    [InlineData(223, 32, 1)] // Threshold boundary should use normal comparison
    public void CircularSequenceComparer_ShouldCompareCorrectly(int x, int y, int expectedResult)
    {
        var comparer = new MessageOrderingService.CircularSequenceComparer();
        var result = comparer.Compare(x, y);
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void ProcessMessageOrder_ShouldProcessContinuousSequence()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = new SparkplugClientOptions { HostApplicationId = "TestHost" };
        var mockLogger = new Mock<ILogger<MessageOrderingService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var service = new MessageOrderingService(memoryCache, options, mockLoggerFactory.Object);

        var message1 = CreateMessageEventArgs(1);
        var message2 = CreateMessageEventArgs(2);
        var message3 = CreateMessageEventArgs(3);

        var result1 = service.ProcessMessageOrder(message1);
        var result2 = service.ProcessMessageOrder(message2);
        var result3 = service.ProcessMessageOrder(message3);

        Assert.Single(result1);
        Assert.Single(result2);
        Assert.Single(result3);
        Assert.Equal(1, result1[0].Payload.Seq);
        Assert.Equal(2, result2[0].Payload.Seq);
        Assert.Equal(3, result3[0].Payload.Seq);

        Assert.True(result1[0].IsSeqConsecutive);
        Assert.False(result1[0].IsCached);
        Assert.True(result2[0].IsSeqConsecutive);
        Assert.False(result2[0].IsCached);
        Assert.True(result3[0].IsSeqConsecutive);
        Assert.False(result3[0].IsCached);
    }

    [Fact]
    public void ProcessMessageOrder_ShouldCacheOutOfOrderMessages()
    {
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3);
        var message2 = CreateMessageEventArgs(2);

        var result1 = _service.ProcessMessageOrder(message1);
        var result3 = _service.ProcessMessageOrder(message3);
        var result2 = _service.ProcessMessageOrder(message2);

        Assert.Single(result1);
        Assert.Empty(result3); // Should be cached
        Assert.Equal(2, result2.Count); // Should process both messages 2 and 3 when the gap is filled
        Assert.Equal(2, result2[0].Payload.Seq);
        Assert.Equal(3, result2[1].Payload.Seq);

        // Verify fields for the immediately processed message
        Assert.True(result1[0].IsSeqConsecutive);
        Assert.False(result1[0].IsCached);

        // Verify fields for the cached and then processed messages
        Assert.True(result2[0].IsSeqConsecutive);
        Assert.False(result2[0].IsCached);
        Assert.True(result2[1].IsSeqConsecutive);
        Assert.True(result2[1].IsCached);
    }

    [Fact]
    public void ProcessMessageOrder_ShouldHandleMultipleOutOfOrderMessages()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = new SparkplugClientOptions { HostApplicationId = "TestHost" };
        var mockLogger = new Mock<ILogger<MessageOrderingService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var service = new MessageOrderingService(memoryCache, options, mockLoggerFactory.Object);

        var message1 = CreateMessageEventArgs(1);
        var message4 = CreateMessageEventArgs(4);
        var message6 = CreateMessageEventArgs(6);
        var message2 = CreateMessageEventArgs(2);
        var message3 = CreateMessageEventArgs(3);
        var message5 = CreateMessageEventArgs(5);

        var result1 = service.ProcessMessageOrder(message1);
        var result4 = service.ProcessMessageOrder(message4);
        var result6 = service.ProcessMessageOrder(message6);
        var result2 = service.ProcessMessageOrder(message2);
        var result3 = service.ProcessMessageOrder(message3);
        var result5 = service.ProcessMessageOrder(message5);

        Assert.Single(result1); // Message 1 processed immediately
        Assert.Empty(result4); // Message 4 cached
        Assert.Empty(result6); // Message 6 cached
        Assert.Single(result2); // Message 2 processed immediately
        Assert.Equal(2, result3.Count); // Messages 3 and 4 processed when the gap is filled
        Assert.Equal(3, result3[0].Payload.Seq);
        Assert.Equal(4, result3[1].Payload.Seq);
        Assert.Equal(2, result5.Count); // Messages 5 and 6 processed when the gap is filled
        Assert.Equal(5, result5[0].Payload.Seq);
        Assert.Equal(6, result5[1].Payload.Seq);

        // Verify fields for immediately processed consecutive messages
        Assert.True(result1[0].IsSeqConsecutive);
        Assert.False(result1[0].IsCached);
        Assert.True(result2[0].IsSeqConsecutive);
        Assert.False(result2[0].IsCached);

        // Verify fields for messages processed from the cache
        Assert.True(result3[0].IsSeqConsecutive);
        Assert.False(result3[0].IsCached);
        Assert.True(result3[1].IsSeqConsecutive);
        Assert.True(result3[1].IsCached);
        Assert.True(result5[0].IsSeqConsecutive);
        Assert.False(result5[0].IsCached);
        Assert.True(result5[1].IsSeqConsecutive);
        Assert.True(result5[1].IsCached);
    }

    [Fact]
    public void ProcessMessageOrder_ShouldHandleSequenceWrapAround()
    {
        var message254 = CreateMessageEventArgs(254);
        var message255 = CreateMessageEventArgs(255);
        var message0 = CreateMessageEventArgs(0);
        var message1 = CreateMessageEventArgs(1);

        var result254 = _service.ProcessMessageOrder(message254);
        var result255 = _service.ProcessMessageOrder(message255);
        var result0 = _service.ProcessMessageOrder(message0);
        var result1 = _service.ProcessMessageOrder(message1);

        Assert.Single(result254);
        Assert.Single(result255);
        Assert.Single(result0); // Should handle wrap-around
        Assert.Single(result1);

        // Verify fields including the sequence wrap-around case
        Assert.True(result254[0].IsSeqConsecutive);
        Assert.False(result254[0].IsCached);
        Assert.True(result255[0].IsSeqConsecutive);
        Assert.False(result255[0].IsCached);
        Assert.True(result0[0].IsSeqConsecutive); // Verify wrap-around sequence is treated as consecutive
        Assert.False(result0[0].IsCached);
        Assert.True(result1[0].IsSeqConsecutive);
        Assert.False(result1[0].IsCached);
    }

    [Fact]
    public void ProcessMessageOrder_ShouldReturnInvalidSequenceNumberMessages()
    {
        var invalidMessageNegative = CreateMessageEventArgs(-1);
        var invalidMessageTooHigh = CreateMessageEventArgs(256);

        var resultNegative = _service.ProcessMessageOrder(invalidMessageNegative);
        var resultTooHigh = _service.ProcessMessageOrder(invalidMessageTooHigh);

        // Verify that invalid sequence number messages are returned
        Assert.Single(resultNegative);
        Assert.Single(resultTooHigh);
        Assert.Same(invalidMessageNegative, resultNegative[0]);
        Assert.Same(invalidMessageTooHigh, resultTooHigh[0]);
    }

    [Fact]
    public void ClearMessageOrderCache_ShouldRemoveCachedItems()
    {
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3); // Will be cached
        _service.ProcessMessageOrder(message1);
        _service.ProcessMessageOrder(message3);

        _service.ClearMessageOrderCache("Group1", "Edge1", "Device1");
        var message2 = CreateMessageEventArgs(2); // Should not process message3 now
        var result2 = _service.ProcessMessageOrder(message2);

        Assert.Single(result2); // Only context2 is processed, context3 was cleared from the cache
        Assert.Equal(2, result2[0].Payload.Seq);
    }

    [Fact]
    public void OnReorderTimeout_ShouldProcessPendingMessages()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = new SparkplugClientOptions { HostApplicationId = "TestHost" };
        var mockLogger = new Mock<ILogger<MessageOrderingService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

        // List to capture pending messages processed by the timeout handler
        List<SparkplugMessageEventArgs>? capturedMessages = null;

        var service = new MessageOrderingService(memoryCache, options, mockLoggerFactory.Object)
        {
            // Set up the delegate to capture pending messages when processed by timeout
            OnPendingMessages = messages =>
            {
                capturedMessages = [.. messages];
                return Task.CompletedTask;
            }
        };

        // Create out-of-order messages to generate pending messages
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3); // This will be cached as pending

        // Process messages to create pending state
        service.ProcessMessageOrder(message1);
        service.ProcessMessageOrder(message3);

        // Simulate timeout by directly invoking the OnReorderTimeout method
        const string timerKey = "Group1:Edge1:Device1";
        service.GetType().GetMethod("OnReorderTimeout", BindingFlags.NonPublic | BindingFlags.Instance)?
            .Invoke(service, [timerKey]);

        // Allow async operation to complete
        Thread.Sleep(100);

        // Verify that pending messages were processed during timeout
        Assert.NotNull(capturedMessages);
        Assert.Single(capturedMessages);
        Assert.Equal(3, capturedMessages[0].Payload.Seq);
    }

    [Fact]
    public async Task OnReorderTimeout_ShouldRemoveTimer()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = new SparkplugClientOptions { HostApplicationId = "TestHost" };
        var mockLogger = new Mock<ILogger<MessageOrderingService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var service = new MessageOrderingService(memoryCache, options, mockLoggerFactory.Object)
        {
            OnPendingMessages = _ => Task.CompletedTask
        };

        // Create messages with sequence numbers 1 and 3 (simulate out of order)
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3);

        // Process messages to create a timer
        service.ProcessMessageOrder(message1);
        service.ProcessMessageOrder(message3);

        // Get the cache key to verify timer removal
        var cacheKey = MessageOrderingService.BuildCacheKey(null, "Group1", "Edge1", "Device1");

        const string timerKey = "Group1:Edge1:Device1";
        service.GetType().GetMethod("OnReorderTimeout", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(service, [timerKey]);

        await Task.Delay(100);

        // Verify the timer was removed from the cache
        Assert.False(memoryCache.TryGetValue($"{cacheKey}_timer", out _));
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

    [Fact]
    public void ProcessMessageOrder_ShouldReturnReplacedMessage_WhenSequenceDuplicate()
    {
        // Create the first message with sequence 2 that will be cached
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3); // Will be cached
        var result1 = _service.ProcessMessageOrder(message1);
        var result3 = _service.ProcessMessageOrder(message3);

        // Verify initial messages were processed/cached correctly
        Assert.Single(result1);
        Assert.Empty(result3); // Message 3 should be cached

        // Create the second message with the same sequence 3 as the cached message
        var duplicateMessage3 = CreateMessageEventArgs(3);
        var resultDuplicate = _service.ProcessMessageOrder(duplicateMessage3);

        // Verify the result contains the original cached message (which was replaced)
        Assert.Single(resultDuplicate);
        Assert.Equal(3, resultDuplicate[0].Payload.Seq);
        Assert.Same(message3, resultDuplicate[0]); // Should return the original cached message
        Assert.True(resultDuplicate[0].IsCached); // Original message should have IsCached = true

        // Now process message 2 to trigger processing of the duplicate message
        var message2 = CreateMessageEventArgs(2);
        var result2 = _service.ProcessMessageOrder(message2);

        // Verify message 2 and the duplicate message 3 are processed
        Assert.Equal(2, result2.Count);
        Assert.Equal(2, result2[0].Payload.Seq);
        Assert.Equal(3, result2[1].Payload.Seq);
        Assert.Same(duplicateMessage3, result2[1]); // Should use the new duplicate message, not the original
    }

    private static SparkplugMessageEventArgs CreateMessageEventArgs(int sequenceNumber)
    {
        // Create payload with specified sequence number
        var payload = new Payload();
        var seqProperty = payload.GetType().GetProperty("Seq");
        seqProperty?.SetValue(payload, sequenceNumber);

        return new SparkplugMessageEventArgs(
            SparkplugVersion.V300,
            SparkplugMessageType.NDATA,
            "Group1",
            "Edge1",
            "Device1",
            payload,
            null!
        );
    }
}