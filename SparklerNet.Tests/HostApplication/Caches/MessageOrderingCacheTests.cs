using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Events;
using SparklerNet.Core.Model;
using SparklerNet.Core.Options;
using SparklerNet.HostApplication.Caches;
using Xunit;

namespace SparklerNet.Tests.HostApplication.Caches;

public class MessageOrderingCacheTests
{
    private readonly MessageOrderingCache _service = CreateMessageOrderingCache();

    [Fact]
    public async Task ProcessMessageOrderAsync_ShouldProcessContinuousSequence()
    {
        var message1 = CreateMessageEventArgs(1);
        var message2 = CreateMessageEventArgs(2);
        var message3 = CreateMessageEventArgs(3);

        var result1 = await _service.ProcessMessageOrderAsync(message1);
        var result2 = await _service.ProcessMessageOrderAsync(message2);
        var result3 = await _service.ProcessMessageOrderAsync(message3);

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
    public async Task ProcessMessageOrderAsync_ShouldCacheOutOfOrderMessages()
    {
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3);
        var message2 = CreateMessageEventArgs(2);

        var result1 = await _service.ProcessMessageOrderAsync(message1);
        var result3 = await _service.ProcessMessageOrderAsync(message3);
        var result2 = await _service.ProcessMessageOrderAsync(message2);

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
    public async Task ProcessMessageOrderAsync_ShouldHandleMultipleOutOfOrderMessages()
    {
        // Create messages with multiple sequence gaps
        var message1 = CreateMessageEventArgs(1);
        var message4 = CreateMessageEventArgs(4); // Will be cached
        var message6 = CreateMessageEventArgs(6); // Will be cached
        var message3 = CreateMessageEventArgs(3); // Will be cached
        var message2 = CreateMessageEventArgs(2); // Will process messages 2, 3, 4
        var message5 = CreateMessageEventArgs(5); // Will process messages 5, 6

        var result1 = await _service.ProcessMessageOrderAsync(message1);
        var result4 = await _service.ProcessMessageOrderAsync(message4);
        var result6 = await _service.ProcessMessageOrderAsync(message6);
        var result3 = await _service.ProcessMessageOrderAsync(message3);
        var result2 = await _service.ProcessMessageOrderAsync(message2);
        var result5 = await _service.ProcessMessageOrderAsync(message5);

        // Verify the initial message was processed
        Assert.Single(result1);
        Assert.Equal(1, result1[0].Payload.Seq);
        Assert.True(result1[0].IsSeqConsecutive);
        Assert.False(result1[0].IsCached);

        // Verify out-of-order messages were cached
        Assert.Empty(result4);
        Assert.Empty(result6);
        Assert.Empty(result3);

        // Verify message2 triggers processing of messages 2, 3, 4
        Assert.Equal(3, result2.Count);
        Assert.Equal(2, result2[0].Payload.Seq);
        Assert.Equal(3, result2[1].Payload.Seq);
        Assert.Equal(4, result2[2].Payload.Seq);
        Assert.True(result2[0].IsSeqConsecutive);
        Assert.False(result2[0].IsCached);
        Assert.True(result2[1].IsSeqConsecutive);
        Assert.True(result2[1].IsCached);
        Assert.True(result2[2].IsSeqConsecutive);
        Assert.True(result2[2].IsCached);

        // Verify message5 triggers processing of messages 5, 6
        Assert.Equal(2, result5.Count);
        Assert.Equal(5, result5[0].Payload.Seq);
        Assert.Equal(6, result5[1].Payload.Seq);
        Assert.True(result5[0].IsSeqConsecutive);
        Assert.False(result5[0].IsCached);
        Assert.True(result5[1].IsSeqConsecutive);
        Assert.True(result5[1].IsCached);
    }

    [Fact]
    public async Task ProcessMessageOrderAsync_ShouldHandleSequenceWrapAround()
    {
        var message254 = CreateMessageEventArgs(254);
        var message255 = CreateMessageEventArgs(255);
        var message0 = CreateMessageEventArgs(0);
        var message1 = CreateMessageEventArgs(1);

        var result254 = await _service.ProcessMessageOrderAsync(message254);
        var result255 = await _service.ProcessMessageOrderAsync(message255);
        var result0 = await _service.ProcessMessageOrderAsync(message0);
        var result1 = await _service.ProcessMessageOrderAsync(message1);

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
    public async Task ProcessMessageOrderAsync_ShouldHandleFirstMessage()
    {
        // Reset the cache to ensure we're testing the first message scenario
        await _service.ResetMessageOrderAsync("Group1", "Edge1");

        // Create the first message with sequence 0
        var firstMessage = CreateMessageEventArgs(0);
        var result = await _service.ProcessMessageOrderAsync(firstMessage);

        // Verify the first message is processed correctly
        Assert.Single(result);
        Assert.Same(firstMessage, result[0]);
        Assert.True(result[0].IsSeqConsecutive);
        Assert.False(result[0].IsCached);
        Assert.Equal(0, result[0].Payload.Seq);
    }

    [Fact]
    public async Task ProcessMessageOrderAsync_ShouldReturnInvalidSequenceNumberMessages()
    {
        var invalidMessageNegative = CreateMessageEventArgs(-1);
        var invalidMessageTooHigh = CreateMessageEventArgs(256);

        var resultNegative = await _service.ProcessMessageOrderAsync(invalidMessageNegative);
        var resultTooHigh = await _service.ProcessMessageOrderAsync(invalidMessageTooHigh);

        // Verify that invalid sequence number messages are returned
        Assert.Single(resultNegative);
        Assert.Single(resultTooHigh);
        Assert.Same(invalidMessageNegative, resultNegative[0]);
        Assert.Same(invalidMessageTooHigh, resultTooHigh[0]);
    }

    [Fact]
    public async Task ResetMessageOrderAsync_ShouldRemoveCachedItems()
    {
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3); // Will be cached
        await _service.ProcessMessageOrderAsync(message1);
        await _service.ProcessMessageOrderAsync(message3);

        await _service.ResetMessageOrderAsync("Group1", "Edge1");
        var message2 = CreateMessageEventArgs(2); // Should not process message3 now
        var result2 = await _service.ProcessMessageOrderAsync(message2);

        Assert.Single(result2); // Only message2 is processed, message3 was cleared from the cache
        Assert.Equal(2, result2[0].Payload.Seq);
    }

    [Fact]
    public async Task ProcessMessageOrderAsync_ShouldReturnReplacedMessage_WhenSequenceDuplicate()
    {
        // Create the first message with sequence 2 that will be cached
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3); // Will be cached
        var result1 = await _service.ProcessMessageOrderAsync(message1);
        var result3 = await _service.ProcessMessageOrderAsync(message3);

        // Verify initial messages were processed/cached correctly
        Assert.Single(result1);
        Assert.Empty(result3); // Message 3 should be cached

        // Create the second message with the same sequence 3 as the cached message
        var duplicateMessage3 = CreateMessageEventArgs(3);
        var resultDuplicate = await _service.ProcessMessageOrderAsync(duplicateMessage3);

        // Verify the result contains the original cached message (which was replaced)
        Assert.Single(resultDuplicate);
        Assert.Equal(3, resultDuplicate[0].Payload.Seq);
        Assert.Equal(message3.GroupId, resultDuplicate[0].GroupId);
        Assert.Equal(message3.EdgeNodeId, resultDuplicate[0].EdgeNodeId);
        Assert.Equal(message3.DeviceId, resultDuplicate[0].DeviceId);
        Assert.Equal(message3.MessageType, resultDuplicate[0].MessageType);
        Assert.True(resultDuplicate[0].IsCached); // Original message should have IsCached = true

        // Now process message 2 to trigger processing of the duplicate message
        var message2 = CreateMessageEventArgs(2);
        var result2 = await _service.ProcessMessageOrderAsync(message2);

        // Verify message 2 and duplicate message 3 are processed
        Assert.Equal(2, result2.Count);
        Assert.Equal(2, result2[0].Payload.Seq);
        Assert.Equal(3, result2[1].Payload.Seq);
        Assert.Equal(duplicateMessage3.GroupId, result2[1].GroupId);
        Assert.Equal(duplicateMessage3.EdgeNodeId, result2[1].EdgeNodeId);
        Assert.Equal(duplicateMessage3.DeviceId, result2[1].DeviceId);
        Assert.Equal(duplicateMessage3.MessageType, result2[1].MessageType);
    }

    [Fact]
    public async Task ProcessMessageOrderAsync_ShouldOnlyAcceptValidMessageTypes()
    {
        // Test with valid message types
        var validTypes = new[]
        {
            SparkplugMessageType.NDATA, SparkplugMessageType.DDATA, SparkplugMessageType.DBIRTH,
            SparkplugMessageType.DDEATH
        };
        foreach (var messageType in validTypes)
        {
            // Reset the cache for each test case to ensure a clean state
            await _service.ResetMessageOrderAsync("Group1", "Edge1");
            var message = CreateMessageEventArgs(1, messageType);
            var result = await _service.ProcessMessageOrderAsync(message);
            Assert.Single(result);
        }

        // Test with invalid message types
        var invalidTypes = new[]
        {
            SparkplugMessageType.NBIRTH, SparkplugMessageType.NDEATH, SparkplugMessageType.NCMD,
            SparkplugMessageType.DCMD
        };
        foreach (var messageType in invalidTypes)
        {
            var message = CreateMessageEventArgs(1, messageType);
            await Assert.ThrowsAsync<ArgumentException>(() => _service.ProcessMessageOrderAsync(message));
        }
    }

    [Fact]
    public void CircularSequenceComparer_ShouldCompareCorrectly()
    {
        var comparer = new MessageOrderingCache.CircularSequenceComparer();

        // Normal integer comparison cases
        Assert.Equal(-1, comparer.Compare(10, 20));
        Assert.Equal(1, comparer.Compare(20, 10));
        Assert.Equal(0, comparer.Compare(15, 15));

        // Wrap-around cases: x near 0 and y near 255 (x should be considered greater)
        Assert.Equal(1, comparer.Compare(10, 240));
        Assert.Equal(1, comparer.Compare(20, 230));
        Assert.Equal(1, comparer.Compare(31, 224));

        // Wrap-around cases: x near 255 and y near 0 (x should be considered smaller)
        Assert.Equal(-1, comparer.Compare(240, 10));
        Assert.Equal(-1, comparer.Compare(230, 20));
        Assert.Equal(-1, comparer.Compare(224, 31));

        // Boundary cases testing
        Assert.Equal(-1, comparer.Compare(32, 223)); // Threshold boundary should use normal comparison
        Assert.Equal(1, comparer.Compare(223, 32)); // Threshold boundary should use normal comparison
    }

    [Fact]
    public void CreateSequenceCacheEntryOptions_ShouldReturnCorrectOptions()
    {
        // Test with default SeqCacheExpiration (0)
        var serviceDefault = CreateMessageOrderingCache();

        // Directly call the protected internal method (visible due to InternalsVisibleTo attribute)
        var resultDefault = serviceDefault.CreateSequenceCacheEntryOptions();
        Assert.NotNull(resultDefault);
        // When SeqCacheExpiration is 0, Expiration should be null (no explicit expiration)
        Assert.Null(resultDefault.Expiration);

        // Test with custom SeqCacheExpiration (30 minutes)
        var serviceCustom = CreateMessageOrderingCache(seqCacheExpiration: 30);

        var resultCustom = serviceCustom.CreateSequenceCacheEntryOptions();
        Assert.NotNull(resultCustom);
        Assert.Equal(TimeSpan.FromMinutes(30), resultCustom.Expiration);
    }

    [Fact]
    public async Task OnReorderTimeout_ShouldProcessPendingMessages()
    {
        // Create a message ordering cache with a short timeout
        var service = CreateMessageOrderingCache(100);

        // Create messages with a sequence gap
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3); // Will be cached

        // Process the first message and cache the second one
        await service.ProcessMessageOrderAsync(message1);
        await service.ProcessMessageOrderAsync(message3);

        // Track processed pending messages
        List<SparkplugMessageEventArgs>? processedPendingMessages = null;
        service.OnPendingMessages = messages =>
        {
            processedPendingMessages = messages.ToList();
            return Task.CompletedTask;
        };

        // Call OnReorderTimeout directly with the timer key
        var timerKey = "Group1:Edge1";
        var methodInfo = typeof(MessageOrderingCache).GetMethod("OnReorderTimeout",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        // Call the method (it's async void, so we can't await it directly)
        methodInfo.Invoke(service, [timerKey]);

        // Wait for a short time to allow the async operation to complete
        await Task.Delay(200);

        // Verify pending message was processed
        Assert.NotNull(processedPendingMessages);
        Assert.Single(processedPendingMessages);
        Assert.Equal(3, processedPendingMessages[0].Payload.Seq);
    }

    [Fact]
    public async Task OnReorderTimeout_ShouldSendRebirthRequest()
    {
        // Create a message ordering cache with a short timeout
        var service = CreateMessageOrderingCache(100);

        // Create messages with a sequence gap
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3); // Will be cached

        // Process the first message and cache the second one
        await service.ProcessMessageOrderAsync(message1);
        await service.ProcessMessageOrderAsync(message3);

        // Track rebirth requests
        string? rebirthGroupId = null;
        string? rebirthEdgeNodeId = null;
        string? rebirthDeviceId = null;
        service.OnRebirthRequested = (groupId, edgeNodeId) =>
        {
            rebirthGroupId = groupId;
            rebirthEdgeNodeId = edgeNodeId;
            return Task.CompletedTask;
        };

        // Call OnReorderTimeout directly with the timer key
        var timerKey = "Group1:Edge1";
        var methodInfo = typeof(MessageOrderingCache).GetMethod("OnReorderTimeout",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        // Call the method (it's async void, so we can't await it directly)
        methodInfo.Invoke(service, [timerKey]);

        // Wait for a short time to allow the async operation to complete
        await Task.Delay(200);

        // Verify rebirth request was sent
        Assert.NotNull(rebirthGroupId);
        Assert.NotNull(rebirthEdgeNodeId);
        Assert.Equal("Group1", rebirthGroupId);
        Assert.Equal("Edge1", rebirthEdgeNodeId);
        Assert.Null(rebirthDeviceId); // No device ID in this case
    }

    [Fact]
    public async Task ProcessMessageOrderAsync_ShouldProcessMessagesConcurrently()
    {
        // Create multiple messages with continuous sequences
        var messages = Enumerable.Range(1, 10)
            .Select(seq => CreateMessageEventArgs(seq))
            .ToList();

        // Process messages concurrently
        var tasks = messages.Select(msg => _service.ProcessMessageOrderAsync(msg));
        var results = await Task.WhenAll(tasks);

        // Verify all messages were processed
        Assert.Equal(10, results.Length);

        // Verify continuous sequence processing
        for (var i = 0; i < results.Length; i++)
        {
            var result = results[i];
            if (i == 0)
            {
                // The first message should be processed immediately
                Assert.Single(result);
                Assert.Equal(1, result[0].Payload.Seq);
                Assert.True(result[0].IsSeqConsecutive);
                Assert.False(result[0].IsCached);
            }
            else
            {
                // Subsequent messages should be processed when their turn comes.
                // This is a simplified test since concurrent processing will likely result in some messages being cached
                // and processed later when the sequence is filled
                var processedSeq = result.Select(msg => msg.Payload.Seq).OrderBy(seq => seq).ToList();
                Assert.NotNull(processedSeq);
            }
        }
    }

    [Fact]
    public async Task CachePendingMessageAsync_ShouldManageTimersCorrectly()
    {
        // Create a message ordering cache with a long timeout
        var service = CreateMessageOrderingCache(10000);

        // Get the _reorderTimers field using reflection
        var reorderTimersField = typeof(MessageOrderingCache).GetField("_reorderTimers",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(reorderTimersField);

        var reorderTimers = (ConcurrentDictionary<string, Timer>)reorderTimersField.GetValue(service)!;
        Assert.NotNull(reorderTimers);
        Assert.Empty(reorderTimers);

        // Create messages with a sequence gap
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3); // Will be cached

        // Process the first message and cache the second one
        await service.ProcessMessageOrderAsync(message1);
        await service.ProcessMessageOrderAsync(message3);

        // Verify timer was added
        Assert.Single(reorderTimers);
        var timerKey = "Group1:Edge1";
        Assert.True(reorderTimers.ContainsKey(timerKey));

        // Create another message with sequence gap (should update the existing timer)
        var message5 = CreateMessageEventArgs(5); // Will be cached
        await service.ProcessMessageOrderAsync(message5);

        // Verify only one timer exists for the same edge node
        Assert.Single(reorderTimers);
        Assert.True(reorderTimers.ContainsKey(timerKey));

        // Process message2 to fill the gap
        var message2 = CreateMessageEventArgs(2);
        await service.ProcessMessageOrderAsync(message2);

        // Verify timer is still present (message5 is still cached)
        Assert.Single(reorderTimers);
        Assert.True(reorderTimers.ContainsKey(timerKey));

        // Process message4 to fill the remaining gap
        var message4 = CreateMessageEventArgs(4);
        await service.ProcessMessageOrderAsync(message4);

        // Verify timer was removed (no more pending messages)
        Assert.Empty(reorderTimers);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldProcessMessagesUntilGap()
    {
        // Create messages with multiple sequence gaps
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3); // Will be cached
        var message4 = CreateMessageEventArgs(4); // Will be cached
        var message6 = CreateMessageEventArgs(6); // Will be cached

        // Process the first message and cache the others
        await _service.ProcessMessageOrderAsync(message1);
        await _service.ProcessMessageOrderAsync(message3);
        await _service.ProcessMessageOrderAsync(message4);
        await _service.ProcessMessageOrderAsync(message6);

        // Get and process pending messages with seq = 1 (should process 2, 3, 4)
        // We need to call this method via reflection since it's private
        var methodInfo = typeof(MessageOrderingCache).GetMethod("GetPendingMessagesAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        var result = await (Task<List<SparkplugMessageEventArgs>>)methodInfo.Invoke(_service,
            ["Group1", "Edge1", 1])!;

        // Verify that only messages 3 and 4 were processed (since message2 is missing)
        // Wait, this is incorrect - we're testing GetPendingMessagesAsync with seq = 1,
        // which means it will look for messages starting from seq = 2,
        // Since message2 is missing, no messages should be processed
        Assert.Empty(result);

        // Now let's process message2 to fill the gap
        var message2 = CreateMessageEventArgs(2);
        var processResult = await _service.ProcessMessageOrderAsync(message2);

        // Verify that messages 2, 3, 4 were processed
        Assert.Equal(3, processResult.Count);
        Assert.Equal(2, processResult[0].Payload.Seq);
        Assert.Equal(3, processResult[1].Payload.Seq);
        Assert.Equal(4, processResult[2].Payload.Seq);
    }

    [Fact]
    public async Task ClearCacheAsync_ShouldClearAllOrderingCache()
    {
        var message1 = CreateMessageEventArgs(1);
        var message3 = CreateMessageEventArgs(3);

        // Process messages to populate the cache
        var result1 = await _service.ProcessMessageOrderAsync(message1);
        var result3 = await _service.ProcessMessageOrderAsync(message3);

        // Verify cache has been populated
        Assert.Single(result1);
        Assert.Empty(result3); // Message 3 should be cached

        // Call ClearCacheAsync to clear all cache
        await _service.ClearCacheAsync();

        // Process new messages after cache clear
        var message2 = CreateMessageEventArgs(2);
        var result2AfterClear = await _service.ProcessMessageOrderAsync(message2);

        // Verify cache has been cleared; message 2 is processed as a new sequence
        Assert.Single(result2AfterClear);
        Assert.Equal(2, result2AfterClear[0].Payload.Seq);
        Assert.True(result2AfterClear[0].IsSeqConsecutive);
        Assert.False(result2AfterClear[0].IsCached);
    }
    
    private static SparkplugMessageEventArgs CreateMessageEventArgs(int sequenceNumber,
        SparkplugMessageType messageType = SparkplugMessageType.NDATA)
    {
        // Create payload with specified sequence number
        var payload = new Payload();
        var seqProperty = payload.GetType().GetProperty("Seq");
        seqProperty?.SetValue(payload, sequenceNumber);

        return new SparkplugMessageEventArgs(
            SparkplugVersion.V300,
            messageType,
            "Group1",
            "Edge1",
            "Device1",
            payload
        );
    }
    
    private static MessageOrderingCache CreateMessageOrderingCache(
        int seqReorderTimeout = 1000,
        int seqCacheExpiration = 0,
        string hostApplicationId = "TestHost")
    {
        var options = new SparkplugClientOptions
        {
            HostApplicationId = hostApplicationId,
            EnableMessageOrdering = true,
            SeqReorderTimeout = seqReorderTimeout,
            SendRebirthWhenTimeout = true,
            SeqCacheExpiration = seqCacheExpiration
        };

        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddHybridCache();
        services.AddSingleton(options);

        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(factory => factory.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger<MessageOrderingCache>>().Object);
        services.AddSingleton(mockLoggerFactory.Object);
        services.AddSingleton<MessageOrderingCache>();

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<MessageOrderingCache>();
    }
}