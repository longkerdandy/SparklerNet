using SparklerNet.Core.Constants;
using SparklerNet.Core.Topics;
using Xunit;

namespace SparklerNet.Tests.Core.Topics;

public class SparkplugTopicParserTests
{
    [Theory]
    [InlineData("spBv1.0/group1/NDATA/edgeNode1/device1", SparkplugVersion.V300, "group1", SparkplugMessageType.NDATA,
        "edgeNode1", "device1", null)]
    [InlineData("spBv1.0/group1/NDATA/edgeNode1", SparkplugVersion.V300, "group1", SparkplugMessageType.NDATA,
        "edgeNode1", null, null)]
    [InlineData("spBv1.0/group1/NBIRTH/edgeNode1", SparkplugVersion.V300, "group1", SparkplugMessageType.NBIRTH,
        "edgeNode1", null, null)]
    [InlineData("spBv1.0/group1/NDEATH/edgeNode1", SparkplugVersion.V300, "group1", SparkplugMessageType.NDEATH,
        "edgeNode1", null, null)]
    [InlineData("spBv1.0/group1/DBIRTH/edgeNode1/device1", SparkplugVersion.V300, "group1", SparkplugMessageType.DBIRTH,
        "edgeNode1", "device1", null)]
    [InlineData("spBv1.0/group1/DDEATH/edgeNode1/device1", SparkplugVersion.V300, "group1", SparkplugMessageType.DDEATH,
        "edgeNode1", "device1", null)]
    [InlineData("spBv1.0/group1/NCMD/edgeNode1", SparkplugVersion.V300, "group1", SparkplugMessageType.NCMD,
        "edgeNode1", null, null)]
    [InlineData("spBv1.0/group1/DCMD/edgeNode1/device1", SparkplugVersion.V300, "group1", SparkplugMessageType.DCMD,
        "edgeNode1", "device1", null)]
    [InlineData("spBv1.0/STATE/host1", SparkplugVersion.V300, null, SparkplugMessageType.STATE, null, null, "host1")]
    public void ParseTopic_ValidTopics_ReturnsCorrectValues(
        string topic,
        SparkplugVersion expectedVersion,
        string? expectedGroupId,
        SparkplugMessageType expectedMessageType,
        string? expectedEdgeNodeId,
        string? expectedDeviceId,
        string? expectedHostId)
    {
        // Test that valid topics are correctly parsed into their components
        var result = SparkplugTopicParser.ParseTopic(topic);
        Assert.Equal(expectedVersion, result.version);
        Assert.Equal(expectedGroupId, result.groupId);
        Assert.Equal(expectedMessageType, result.messageType);
        Assert.Equal(expectedEdgeNodeId, result.edgeNodeId);
        Assert.Equal(expectedDeviceId, result.deviceId);
        Assert.Equal(expectedHostId, result.hostId);
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(NotSupportedException))]
    [InlineData("invalid-topic-format", typeof(NotSupportedException))]
    [InlineData("spBv1.0/group1/UNSUPPORTED/edgeNode1", typeof(NotSupportedException))]
    [InlineData("unsupported-namespace/group1/NDATA/edgeNode1", typeof(NotSupportedException))]
    public void ParseTopic_InvalidTopics_ThrowsExpectedException(string? topic, Type expectedExceptionType)
    {
        // Test that invalid topics throw the expected exceptions
        Assert.Throws(expectedExceptionType, () => SparkplugTopicParser.ParseTopic(topic!));
    }

    [Fact]
    public void ParseTopic_CaseInsensitiveMatching()
    {
        // Using uppercase for message type and namespace
        const string topic = "SPBV1.0/GROUP1/ndata/edgeNode1";
        var result = SparkplugTopicParser.ParseTopic(topic);
        Assert.Equal(SparkplugVersion.V300, result.version);
        Assert.Equal("GROUP1", result.groupId); // Note: groupId is case-sensitive as it's a value
        Assert.Equal(SparkplugMessageType.NDATA, result.messageType); // Message type should be case-insensitive
    }
}