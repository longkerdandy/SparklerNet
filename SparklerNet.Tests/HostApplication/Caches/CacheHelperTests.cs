using Xunit;
using SparklerNet.HostApplication.Caches;

namespace SparklerNet.Tests.HostApplication.Caches;

public class CacheHelperTests
{
    [Theory]
    [InlineData("prefix:", "group1", "edge1", "device1", "prefix:group1:edge1:device1")]
    [InlineData("status:", "group2", "edge2", "device2", "status:group2:edge2:device2")]
    [InlineData(null, "group3", "edge3", "device3", "group3:edge3:device3")]
    [InlineData("", "group4", "edge4", "device4", "group4:edge4:device4")]
    public void BuildCacheKey_WithDeviceId_ReturnsCorrectFormat(string? prefix, string groupId, string edgeNodeId, string? deviceId, string expected)
    {
        var result = CacheHelper.BuildCacheKey(prefix, groupId, edgeNodeId, deviceId);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("prefix:", "group1", "edge1", null, "prefix:group1:edge1")]
    [InlineData("status:", "group2", "edge2", null, "status:group2:edge2")]
    [InlineData(null, "group3", "edge3", null, "group3:edge3")]
    [InlineData("", "group4", "edge4", null, "group4:edge4")]
    public void BuildCacheKey_WithoutDeviceId_ReturnsCorrectFormat(string? prefix, string groupId, string edgeNodeId, string? deviceId, string expected)
    {
        var result = CacheHelper.BuildCacheKey(prefix, groupId, edgeNodeId, deviceId);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetSemaphore_ReturnsSameInstanceForSameKey()
    {
        var semaphore1 = CacheHelper.GetSemaphore("group1", "edge1", "device1");
        var semaphore2 = CacheHelper.GetSemaphore("group1", "edge1", "device1");
        
        Assert.Same(semaphore1, semaphore2);
    }

    [Fact]
    public void GetSemaphore_ReturnsDifferentInstancesForDifferentKeys()
    {
        var semaphore1 = CacheHelper.GetSemaphore("group1", "edge1", "device1");
        var semaphore2 = CacheHelper.GetSemaphore("group2", "edge2", "device2");
        
        Assert.NotSame(semaphore1, semaphore2);
    }

    [Fact]
    public void GetSemaphore_WithAndWithoutDeviceId_ReturnsDifferentInstances()
    {
        var semaphore1 = CacheHelper.GetSemaphore("group1", "edge1", "device1");
        var semaphore2 = CacheHelper.GetSemaphore("group1", "edge1", null);
        
        Assert.NotSame(semaphore1, semaphore2);
    }
}