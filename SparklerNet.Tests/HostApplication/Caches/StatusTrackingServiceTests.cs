using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using SparklerNet.HostApplication.Caches;
using Xunit;

namespace SparklerNet.Tests.HostApplication.Caches;

[SuppressMessage("ReSharper", "ConvertToConstant.Local")]
public class StatusTrackingServiceTests
{
    private readonly StatusTrackingService _statusService;

    public StatusTrackingServiceTests()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddHybridCache();
        services.AddSingleton<StatusTrackingService>();
        var serviceProvider = services.BuildServiceProvider();
        _statusService = serviceProvider.GetRequiredService<StatusTrackingService>();
    }

    [Fact]
    public async Task IsEndpointOnline_WhenNoStatusInCache_ReturnsFalse()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";
        var deviceId = "device1";

        var result = await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsEndpointOnline_WithOnlineStatus_ReturnsTrue()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";

        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, true, 1, 1000);

        var edgeNodeStatus = await _statusService.IsEndpointOnline(groupId, edgeNodeId, null);

        Assert.True(edgeNodeStatus);
    }

    [Fact]
    public async Task IsEndpointOnline_WithOfflineStatus_ReturnsFalse()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";
        var deviceId = "device1";

        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, false, 1, 1000);

        var result = await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsEndpointOnline_WithNullDeviceId_ReturnsCorrectStatus()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";

        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, true, 1, 1000);

        var result = await _statusService.IsEndpointOnline(groupId, edgeNodeId, null);

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateEdgeNodeOnlineStatus_WhenSettingOnlineStatus_CachesStatus()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";
        var isOnline = true;
        var bdSeq = 1;
        var timestamp = 1000L;

        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, isOnline, bdSeq, timestamp);

        var status = await _statusService.IsEndpointOnline(groupId, edgeNodeId, null);

        Assert.True(status);
    }

    [Fact]
    public async Task UpdateEdgeNodeOnlineStatus_WhenNewerTimestampUpdatesOlderTimestamp()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";

        // Set initial status
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, true, 1, 1000);

        // Update with newer timestamp
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, true, 2, 2000);

        var status = await _statusService.IsEndpointOnline(groupId, edgeNodeId, null);

        Assert.True(status);
    }

    [Fact]
    public async Task UpdateEdgeNodeOnlineStatus_WhenOlderTimestampDoesNotUpdateNewerTimestamp()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";

        // Set initial status with newer timestamp
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, true, 2, 2000);

        // Try to update with an older timestamp
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, false, 1, 1000);

        // Should remain online
        var status = await _statusService.IsEndpointOnline(groupId, edgeNodeId, null);

        Assert.True(status);
    }

    [Fact]
    public async Task UpdateEdgeNodeOnlineStatus_WhenSettingOfflineStatusWithSameBdSeqUpdatesOnlineStatus()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";
        var bdSeq = 1;

        // Set online status
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, true, bdSeq, 1000);

        // Set offline status with same bdSeq
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, false, bdSeq, 2000);

        var status = await _statusService.IsEndpointOnline(groupId, edgeNodeId, null);

        Assert.False(status);
    }

    [Fact]
    public async Task UpdateEdgeNodeOnlineStatus_WhenSettingOfflineStatusWithNewerTimestampUpdatesOfflineStatus()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";

        // Set offline status
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, false, 1, 1000);

        // Update offline status with newer timestamp
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, false, 2, 2000);

        var status = await _statusService.IsEndpointOnline(groupId, edgeNodeId, null);

        Assert.False(status);
    }

    [Fact]
    public async Task UpdateDeviceOnlineStatus_WhenSettingOnlineStatus_CachesStatus()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";
        var deviceId = "device1";
        var isOnline = true;
        var timestamp = 1000L;

        await _statusService.UpdateDeviceOnlineStatus(groupId, edgeNodeId, deviceId, isOnline, timestamp);

        var status = await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId);

        Assert.True(status);
    }

    [Fact]
    public async Task UpdateDeviceOnlineStatus_WhenNewerTimestampUpdatesOlderTimestamp()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";
        var deviceId = "device1";

        // Set initial status
        await _statusService.UpdateDeviceOnlineStatus(groupId, edgeNodeId, deviceId, false, 1000);

        // Update with newer timestamp
        await _statusService.UpdateDeviceOnlineStatus(groupId, edgeNodeId, deviceId, true, 2000);

        var status = await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId);

        Assert.True(status);
    }

    [Fact]
    public async Task UpdateDeviceOnlineStatus_WhenOlderTimestampDoesNotUpdateNewerTimestamp()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";
        var deviceId = "device1";

        // Set initial status with newer timestamp
        await _statusService.UpdateDeviceOnlineStatus(groupId, edgeNodeId, deviceId, true, 2000);

        // Try to update with an older timestamp
        await _statusService.UpdateDeviceOnlineStatus(groupId, edgeNodeId, deviceId, false, 1000);

        // Should remain online
        var status = await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId);

        Assert.True(status);
    }

    [Fact]
    public async Task UpdateDeviceOnlineStatus_DeviceStatusIsIndependentOfEdgeNodeStatus()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";
        var deviceId = "device1";

        // Set edge node as offline
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, false, 1, 1000);

        // Set device as online
        await _statusService.UpdateDeviceOnlineStatus(groupId, edgeNodeId, deviceId, true, 2000);

        // Check statuses separately
        var edgeNodeStatus = await _statusService.IsEndpointOnline(groupId, edgeNodeId, null);
        var deviceStatus = await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId);

        Assert.False(edgeNodeStatus);
        Assert.True(deviceStatus);
    }

    [Fact]
    public async Task WhenEdgeNodeGoesOffline_AssociatedDevicesBecomeOffline()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";
        var deviceId = "device1";

        // Set EdgeNode online
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, true, 1, 1000);

        // Set Device online
        await _statusService.UpdateDeviceOnlineStatus(groupId, edgeNodeId, deviceId, true, 2000);

        // Device should be online when EdgeNode is online
        var deviceStatusBefore = await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId);
        Assert.True(deviceStatusBefore, "Device should be online when EdgeNode is online");

        // Set EdgeNode offline
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, false, 1, 3000);

        // Device should now be offline due to EdgeNode going offline
        var deviceStatusAfter = await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId);
        Assert.False(deviceStatusAfter, "Device should be offline when EdgeNode goes offline");
    }

    [Fact]
    public async Task WhenEdgeNodeComesOnlineAgain_DeviceStatusRequiresReset()
    {
        var groupId = "group20";
        var edgeNodeId = "edgeNode20";
        var deviceId = "device20";

        // Set EdgeNode and Device online
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, true, 1, 1000);
        await _statusService.UpdateDeviceOnlineStatus(groupId, edgeNodeId, deviceId, true, 2000);

        // Verify both are online
        Assert.True(await _statusService.IsEndpointOnline(groupId, edgeNodeId, null), "EdgeNode should be online");
        Assert.True(await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId), "Device should be online");

        // Set EdgeNode offline and verify the Device becomes offline
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, false, 1, 3000);
        Assert.False(await _statusService.IsEndpointOnline(groupId, edgeNodeId, null), "EdgeNode should be offline");
        Assert.False(await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId),
            "Device should be offline when EdgeNode is offline");

        // Set EdgeNode online again
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, true, 1, 4000);
        Assert.True(await _statusService.IsEndpointOnline(groupId, edgeNodeId, null),
            "EdgeNode should be online after reconnection");
        Assert.False(await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId),
            "Device should remain offline");
    }

    [Fact]
    public async Task WhenEdgeNodeGoesOffline_MultipleDevicesBecomeOfflineSimultaneously()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";
        var deviceId1 = "device1";
        var deviceId2 = "device2";

        // Set EdgeNode online
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, true, 1, 1000);

        // Set multiple Devices online
        await _statusService.UpdateDeviceOnlineStatus(groupId, edgeNodeId, deviceId1, true, 2000);
        await _statusService.UpdateDeviceOnlineStatus(groupId, edgeNodeId, deviceId2, true, 3000);

        // Verify all are online
        Assert.True(await _statusService.IsEndpointOnline(groupId, edgeNodeId, null), "EdgeNode should be online");
        Assert.True(await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId1),
            "First device should be online");
        Assert.True(await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId2),
            "Second device should be online");

        // Set EdgeNode offline
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, false, 1, 4000);

        // All devices should now be offline due to EdgeNode going offline
        Assert.False(await _statusService.IsEndpointOnline(groupId, edgeNodeId, null), "EdgeNode should be offline");
        Assert.False(await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId1),
            "First device should be offline");
        Assert.False(await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId2),
            "Second device should be offline");
    }

    [Fact]
    public async Task TestMixedEdgeNodeAndDeviceStatusUpdates_Concurrent()
    {
        const string groupId = "test-group";
        const string edgeNodeId = "test-edge-node";
        const string deviceId = "test-device";

        var completedEdgeNodeTasks = 0;
        var completedDeviceTasks = 0;
        var errors = new List<Exception>();

        // Create mixed edge node and device update tasks
        var edgeNodeTasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            try
            {
                await _statusService.UpdateEdgeNodeOnlineStatus(
                    groupId,
                    edgeNodeId,
                    true,
                    1,
                    DateTimeOffset.Now.ToUnixTimeMilliseconds());
                Interlocked.Increment(ref completedEdgeNodeTasks);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        var deviceTasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            try
            {
                await _statusService.UpdateDeviceOnlineStatus(
                    groupId,
                    edgeNodeId,
                    deviceId,
                    true,
                    DateTimeOffset.Now.ToUnixTimeMilliseconds());
                Interlocked.Increment(ref completedDeviceTasks);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        // Run all tasks concurrently
        var allTasks = edgeNodeTasks.Concat(deviceTasks);
        await Task.WhenAll(allTasks);

        // Verify no errors
        Assert.Empty(errors);
        Assert.Equal(5, completedEdgeNodeTasks);
        Assert.Equal(5, completedDeviceTasks);

        // Verify statuses were updated correctly
        Assert.True(await _statusService.IsEndpointOnline(groupId, edgeNodeId, null));
        Assert.True(await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId));
    }
    
    [Fact]
    public async Task ClearCacheAsync_ShouldClearAllStatusCache()
    {
        var groupId = "group1";
        var edgeNodeId = "edgeNode1";
        var deviceId = "device1";

        // Set edge node online
        await _statusService.UpdateEdgeNodeOnlineStatus(groupId, edgeNodeId, true, 1, 1000);
        // Set device online
        await _statusService.UpdateDeviceOnlineStatus(groupId, edgeNodeId, deviceId, true, 2000);

        // Verify statuses are correctly cached
        Assert.True(await _statusService.IsEndpointOnline(groupId, edgeNodeId, null), "EdgeNode should be online before cache clear");
        Assert.True(await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId), "Device should be online before cache clear");

        // Call ClearCacheAsync method
        await _statusService.ClearCacheAsync();

        // Verify cache is cleared, all statuses return default value false
        Assert.False(await _statusService.IsEndpointOnline(groupId, edgeNodeId, null), "EdgeNode should be offline after cache clear");
        Assert.False(await _statusService.IsEndpointOnline(groupId, edgeNodeId, deviceId), "Device should be offline after cache clear");
    }
}