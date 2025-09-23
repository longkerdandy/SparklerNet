using SparklerNet.Core.Constants;
using SparklerNet.Core.Topics;
using Xunit;

namespace SparklerNet.Tests.Core.Topics;

public class SparkplugTopicFactoryTests
{
    [Fact]
    public void CreateSparkplugWildcardTopic_ValidVersion_ReturnsCorrectTopic()
    {
        const SparkplugVersion version = SparkplugVersion.V300;
        var result = SparkplugTopicFactory.CreateSparkplugWildcardTopic(version);
        Assert.Equal("spBv1.0/#", result);
    }

    [Fact]
    public void CreateStateTopic_ValidVersionAndHostId_ReturnsCorrectTopic()
    {
        const SparkplugVersion version = SparkplugVersion.V300;
        const string hostId = "host1";
        var result = SparkplugTopicFactory.CreateStateTopic(version, hostId);
        Assert.Equal("spBv1.0/STATE/host1", result);
    }

    [Fact]
    public void CreateStateTopic_EmptyHostId_ReturnsTopicWithEmptySegment()
    {
        const SparkplugVersion version = SparkplugVersion.V300;
        const string hostId = "";
        var result = SparkplugTopicFactory.CreateStateTopic(version, hostId);
        Assert.Equal("spBv1.0/STATE/", result);
    }

    [Fact]
    public void CreateEdgeNodeTopic_ValidParameters_ReturnsCorrectTopic()
    {
        const SparkplugVersion version = SparkplugVersion.V300;
        const string groupId = "group1";
        const SparkplugMessageType messageType = SparkplugMessageType.NBIRTH;
        const string edgeNodeId = "edgeNode1";
        var result = SparkplugTopicFactory.CreateEdgeNodeTopic(version, groupId, messageType, edgeNodeId);
        Assert.Equal("spBv1.0/group1/NBIRTH/edgeNode1", result);
    }

    [Fact]
    public void CreateEdgeNodeTopic_DifferentMessageType_ReturnsCorrectTopic()
    {
        const SparkplugVersion version = SparkplugVersion.V300;
        const string groupId = "group1";
        const SparkplugMessageType messageType = SparkplugMessageType.NDATA;
        const string edgeNodeId = "edgeNode1";
        var result = SparkplugTopicFactory.CreateEdgeNodeTopic(version, groupId, messageType, edgeNodeId);
        Assert.Equal("spBv1.0/group1/NDATA/edgeNode1", result);
    }

    [Fact]
    public void CreateEdgeNodeTopic_EmptyGroupOrNodeId_ReturnsTopicWithEmptySegments()
    {
        const SparkplugVersion version = SparkplugVersion.V300;
        const string groupId = "";
        const SparkplugMessageType messageType = SparkplugMessageType.NBIRTH;
        const string edgeNodeId = "";
        var result = SparkplugTopicFactory.CreateEdgeNodeTopic(version, groupId, messageType, edgeNodeId);
        Assert.Equal("spBv1.0//NBIRTH/", result);
    }

    [Fact]
    public void CreateDeviceTopic_ValidParameters_ReturnsCorrectTopic()
    {
        const SparkplugVersion version = SparkplugVersion.V300;
        const string groupId = "group1";
        const SparkplugMessageType messageType = SparkplugMessageType.DBIRTH;
        const string edgeNodeId = "edgeNode1";
        const string deviceId = "device1";
        var result = SparkplugTopicFactory.CreateDeviceTopic(version, groupId, messageType, edgeNodeId, deviceId);
        Assert.Equal("spBv1.0/group1/DBIRTH/edgeNode1/device1", result);
    }

    [Fact]
    public void CreateDeviceTopic_DifferentMessageType_ReturnsCorrectTopic()
    {
        const SparkplugVersion version = SparkplugVersion.V300;
        const string groupId = "group1";
        const SparkplugMessageType messageType = SparkplugMessageType.DDATA;
        const string edgeNodeId = "edgeNode1";
        const string deviceId = "device1";
        var result = SparkplugTopicFactory.CreateDeviceTopic(version, groupId, messageType, edgeNodeId, deviceId);
        Assert.Equal("spBv1.0/group1/DDATA/edgeNode1/device1", result);
    }

    [Fact]
    public void CreateDeviceTopic_EmptyGroupNodeOrDeviceId_ReturnsTopicWithEmptySegments()
    {
        const SparkplugVersion version = SparkplugVersion.V300;
        const string groupId = "";
        const SparkplugMessageType messageType = SparkplugMessageType.DBIRTH;
        const string edgeNodeId = "";
        const string deviceId = "";
        var result = SparkplugTopicFactory.CreateDeviceTopic(version, groupId, messageType, edgeNodeId, deviceId);
        Assert.Equal("spBv1.0//DBIRTH//", result);
    }

    [Theory]
    [InlineData("group1", "edgeNode1", "device1", SparkplugMessageType.NCMD)]
    [InlineData("production", "line1", "sensor1", SparkplugMessageType.DCMD)]
    [InlineData("test_group", "test_node", "test_device", SparkplugMessageType.NDEATH)]
    public void CreateTopics_WithDifferentParameters_ReturnsCorrectTopics(string groupId, string edgeNodeId,
        string deviceId, SparkplugMessageType messageType)
    {
        const SparkplugVersion version = SparkplugVersion.V300;
        const string hostId = "test_host";

        var wildcardTopic = SparkplugTopicFactory.CreateSparkplugWildcardTopic(version);
        var stateTopic = SparkplugTopicFactory.CreateStateTopic(version, hostId);
        var edgeNodeTopic = SparkplugTopicFactory.CreateEdgeNodeTopic(version, groupId, messageType, edgeNodeId);
        var deviceTopic = SparkplugTopicFactory.CreateDeviceTopic(version, groupId, messageType, edgeNodeId, deviceId);

        Assert.Equal("spBv1.0/#", wildcardTopic);
        Assert.Equal("spBv1.0/STATE/test_host", stateTopic);
        Assert.Equal($"spBv1.0/{groupId}/{messageType.ToString()}/{edgeNodeId}", edgeNodeTopic);
        Assert.Equal($"spBv1.0/{groupId}/{messageType.ToString()}/{edgeNodeId}/{deviceId}", deviceTopic);
    }
}