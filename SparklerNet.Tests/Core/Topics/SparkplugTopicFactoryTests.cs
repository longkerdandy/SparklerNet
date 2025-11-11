using SparklerNet.Core.Constants;
using SparklerNet.Core.Topics;
using Xunit;

namespace SparklerNet.Tests.Core.Topics;

public class SparkplugTopicFactoryTests
{
    [Theory]
    [InlineData(SparkplugVersion.V300, "spBv1.0/#")]
    public void CreateSparkplugWildcardTopic_ValidVersion_ReturnsCorrectTopic(SparkplugVersion version,
        string expectedTopic)
    {
        // Test that a valid version returns the correct wildcard topic
        var result = SparkplugTopicFactory.CreateSparkplugWildcardTopic(version);
        Assert.Equal(expectedTopic, result);
    }

    [Theory]
    [InlineData(SparkplugVersion.V300, "host1", "spBv1.0/STATE/host1")]
    [InlineData(SparkplugVersion.V300, "", "spBv1.0/STATE/")]
    public void CreateStateTopic_WithVariousHostIds_ReturnsCorrectTopic(SparkplugVersion version, string hostId,
        string expectedTopic)
    {
        // Test that state topics are correctly generated with different host IDs
        var result = SparkplugTopicFactory.CreateStateTopic(version, hostId);
        Assert.Equal(expectedTopic, result);
    }

    [Theory]
    [InlineData(SparkplugVersion.V300, "group1", SparkplugMessageType.NBIRTH, "edgeNode1",
        "spBv1.0/group1/NBIRTH/edgeNode1")]
    [InlineData(SparkplugVersion.V300, "group1", SparkplugMessageType.NDATA, "edgeNode1",
        "spBv1.0/group1/NDATA/edgeNode1")]
    [InlineData(SparkplugVersion.V300, "", SparkplugMessageType.NBIRTH, "", "spBv1.0//NBIRTH/")]
    public void CreateEdgeNodeTopic_WithVariousParameters_ReturnsCorrectTopic(SparkplugVersion version, string groupId,
        SparkplugMessageType messageType, string edgeNodeId, string expectedTopic)
    {
        // Test that edge node topics are correctly generated with different parameters
        var result = SparkplugTopicFactory.CreateEdgeNodeTopic(version, groupId, messageType, edgeNodeId);
        Assert.Equal(expectedTopic, result);
    }

    [Theory]
    [InlineData(SparkplugVersion.V300, "group1", SparkplugMessageType.DBIRTH, "edgeNode1", "device1",
        "spBv1.0/group1/DBIRTH/edgeNode1/device1")]
    [InlineData(SparkplugVersion.V300, "group1", SparkplugMessageType.DDATA, "edgeNode1", "device1",
        "spBv1.0/group1/DDATA/edgeNode1/device1")]
    [InlineData(SparkplugVersion.V300, "", SparkplugMessageType.DBIRTH, "", "", "spBv1.0//DBIRTH//")]
    public void CreateDeviceTopic_WithVariousParameters_ReturnsCorrectTopic(SparkplugVersion version, string groupId,
        SparkplugMessageType messageType, string edgeNodeId, string deviceId, string expectedTopic)
    {
        // Test that device topics are correctly generated with different parameters
        var result = SparkplugTopicFactory.CreateDeviceTopic(version, groupId, messageType, edgeNodeId, deviceId);
        Assert.Equal(expectedTopic, result);
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