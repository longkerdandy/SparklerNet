using SparklerNet.Core.Constants;
using SparklerNet.Core.Topics;
using Xunit;

namespace SparklerNet.Tests.Core.Topics;

public class SparkplugTopicParserTests
{
    [Fact]
    public void ParseTopic_RegularMessageWithDevice_ReturnsCorrectValues()
    {
        const string topic = "spBv1.0/group1/NDATA/edgeNode1/device1";
        var result = SparkplugTopicParser.ParseTopic(topic);
        Assert.Equal(SparkplugVersion.V300, result.version);
        Assert.Equal("group1", result.groupId);
        Assert.Equal(SparkplugMessageType.NDATA, result.messageType);
        Assert.Equal("edgeNode1", result.edgeNodeId);
        Assert.Equal("device1", result.deviceId);
        Assert.Null(result.hostId);
    }

    [Fact]
    public void ParseTopic_RegularMessageWithoutDevice_ReturnsCorrectValues()
    {
        const string topic = "spBv1.0/group1/NDATA/edgeNode1";
        var result = SparkplugTopicParser.ParseTopic(topic);
        Assert.Equal(SparkplugVersion.V300, result.version);
        Assert.Equal("group1", result.groupId);
        Assert.Equal(SparkplugMessageType.NDATA, result.messageType);
        Assert.Equal("edgeNode1", result.edgeNodeId);
        Assert.Null(result.deviceId);
        Assert.Null(result.hostId);
    }

    [Fact]
    public void ParseTopic_NBirthMessage_ReturnsCorrectValues()
    {
        const string topic = "spBv1.0/group1/NBIRTH/edgeNode1";
        var result = SparkplugTopicParser.ParseTopic(topic);
        Assert.Equal(SparkplugVersion.V300, result.version);
        Assert.Equal("group1", result.groupId);
        Assert.Equal(SparkplugMessageType.NBIRTH, result.messageType);
        Assert.Equal("edgeNode1", result.edgeNodeId);
        Assert.Null(result.deviceId);
        Assert.Null(result.hostId);
    }

    [Fact]
    public void ParseTopic_NDeathMessage_ReturnsCorrectValues()
    {
        const string topic = "spBv1.0/group1/NDEATH/edgeNode1";
        var result = SparkplugTopicParser.ParseTopic(topic);
        Assert.Equal(SparkplugVersion.V300, result.version);
        Assert.Equal("group1", result.groupId);
        Assert.Equal(SparkplugMessageType.NDEATH, result.messageType);
        Assert.Equal("edgeNode1", result.edgeNodeId);
        Assert.Null(result.deviceId);
        Assert.Null(result.hostId);
    }

    [Fact]
    public void ParseTopic_DBirthMessage_ReturnsCorrectValues()
    {
        const string topic = "spBv1.0/group1/DBIRTH/edgeNode1/device1";
        var result = SparkplugTopicParser.ParseTopic(topic);
        Assert.Equal(SparkplugVersion.V300, result.version);
        Assert.Equal("group1", result.groupId);
        Assert.Equal(SparkplugMessageType.DBIRTH, result.messageType);
        Assert.Equal("edgeNode1", result.edgeNodeId);
        Assert.Equal("device1", result.deviceId);
        Assert.Null(result.hostId);
    }

    [Fact]
    public void ParseTopic_DDeathMessage_ReturnsCorrectValues()
    {
        const string topic = "spBv1.0/group1/DDEATH/edgeNode1/device1";
        var result = SparkplugTopicParser.ParseTopic(topic);
        Assert.Equal(SparkplugVersion.V300, result.version);
        Assert.Equal("group1", result.groupId);
        Assert.Equal(SparkplugMessageType.DDEATH, result.messageType);
        Assert.Equal("edgeNode1", result.edgeNodeId);
        Assert.Equal("device1", result.deviceId);
        Assert.Null(result.hostId);
    }

    [Fact]
    public void ParseTopic_NCmdMessage_ReturnsCorrectValues()
    {
        const string topic = "spBv1.0/group1/NCMD/edgeNode1";
        var result = SparkplugTopicParser.ParseTopic(topic);
        Assert.Equal(SparkplugVersion.V300, result.version);
        Assert.Equal("group1", result.groupId);
        Assert.Equal(SparkplugMessageType.NCMD, result.messageType);
        Assert.Equal("edgeNode1", result.edgeNodeId);
        Assert.Null(result.deviceId);
        Assert.Null(result.hostId);
    }

    [Fact]
    public void ParseTopic_DCmdMessage_ReturnsCorrectValues()
    {
        const string topic = "spBv1.0/group1/DCMD/edgeNode1/device1";
        var result = SparkplugTopicParser.ParseTopic(topic);
        Assert.Equal(SparkplugVersion.V300, result.version);
        Assert.Equal("group1", result.groupId);
        Assert.Equal(SparkplugMessageType.DCMD, result.messageType);
        Assert.Equal("edgeNode1", result.edgeNodeId);
        Assert.Equal("device1", result.deviceId);
        Assert.Null(result.hostId);
    }

    [Fact]
    public void ParseTopic_StateMessage_ReturnsCorrectValues()
    {
        const string topic = "spBv1.0/STATE/host1";
        var result = SparkplugTopicParser.ParseTopic(topic);
        Assert.Equal(SparkplugVersion.V300, result.version);
        Assert.Null(result.groupId);
        Assert.Equal(SparkplugMessageType.STATE, result.messageType);
        Assert.Null(result.edgeNodeId);
        Assert.Null(result.deviceId);
        Assert.Equal("host1", result.hostId);
    }

    [Fact]
    public void ParseTopic_NullTopic_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SparkplugTopicParser.ParseTopic(null!));
    }

    [Fact]
    public void ParseTopic_EmptyTopic_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => SparkplugTopicParser.ParseTopic(""));
    }

    [Fact]
    public void ParseTopic_InvalidFormat_ThrowsNotSupportedException()
    {
        const string invalidTopic = "invalid-topic-format";
        Assert.Throws<NotSupportedException>(() => SparkplugTopicParser.ParseTopic(invalidTopic));
    }

    [Fact]
    public void ParseTopic_UnsupportedMessageType_ThrowsNotSupportedException()
    {
        const string topicWithUnsupportedType = "spBv1.0/group1/UNSUPPORTED/edgeNode1";
        Assert.Throws<NotSupportedException>(() => SparkplugTopicParser.ParseTopic(topicWithUnsupportedType));
    }

    [Fact]
    public void ParseTopic_UnsupportedNamespace_ThrowsNotSupportedException()
    {
        const string topicWithUnsupportedNamespace = "unsupported-namespace/group1/NDATA/edgeNode1";
        Assert.Throws<NotSupportedException>(() => SparkplugTopicParser.ParseTopic(topicWithUnsupportedNamespace));
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