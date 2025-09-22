using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;
using ProtoMetaData = SparklerNet.Core.Protobuf.Payload.Types.MetaData;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class MetaDataConverterTests
{
    [Fact]
    public void ToProtoMetaData_NullMetaData_ThrowsArgumentNullException()
    {
        MetaData metaData = null!;
        Assert.Throws<ArgumentNullException>(() => metaData.ToProtoMetaData());
    }

    [Fact]
    public void ToProtoMetaData_EmptyMetaData_ReturnsEmptyProtoMetaData()
    {
        var metaData = new MetaData();
        var protoMetaData = metaData.ToProtoMetaData();

        Assert.NotNull(protoMetaData);
        Assert.False(protoMetaData.IsMultiPart);
        Assert.Equal(string.Empty, protoMetaData.ContentType);
        Assert.Equal(0ul, protoMetaData.Size);
        Assert.Equal(0ul, protoMetaData.Seq);
        Assert.Equal(string.Empty, protoMetaData.FileName);
        Assert.Equal(string.Empty, protoMetaData.FileType);
        Assert.Equal(string.Empty, protoMetaData.Md5);
    }

    [Fact]
    public void ToProtoMetaData_BasicMetaData_ConvertsCorrectly()
    {
        var metaData = new MetaData
        {
            IsMultiPart = true,
            ContentType = "application/json",
            Size = 1024,
            Seq = 42,
            FileName = "test.json",
            FileType = "json",
            Md5 = "abc123def456",
            Description = "Test description"
        };

        var protoMetaData = metaData.ToProtoMetaData();

        Assert.NotNull(protoMetaData);
        Assert.True(protoMetaData.IsMultiPart);
        Assert.Equal("application/json", protoMetaData.ContentType);
        Assert.Equal(1024ul, protoMetaData.Size);
        Assert.Equal(42ul, protoMetaData.Seq);
        Assert.Equal("test.json", protoMetaData.FileName);
        Assert.Equal("json", protoMetaData.FileType);
        Assert.Equal("abc123def456", protoMetaData.Md5);
        Assert.Equal("Test description", protoMetaData.Description);
    }

    [Fact]
    public void ToMetaData_NullProtoMetaData_ThrowsArgumentNullException()
    {
        ProtoMetaData protoMetaData = null!;
        Assert.Throws<ArgumentNullException>(() => protoMetaData.ToMetaData());
    }

    [Fact]
    public void ToMetaData_EmptyProtoMetaData_ReturnsEmptyMetaData()
    {
        var protoMetaData = new ProtoMetaData();
        var metaData = protoMetaData.ToMetaData();

        Assert.NotNull(metaData);
        Assert.False(metaData.IsMultiPart);
        Assert.Equal(string.Empty, metaData.ContentType);
        Assert.Equal(0ul, metaData.Size);
        Assert.Equal(0, metaData.Seq);
        Assert.Equal(string.Empty, metaData.FileName);
        Assert.Equal(string.Empty, metaData.FileType);
        Assert.Equal(string.Empty, metaData.Md5);
        Assert.Equal(string.Empty, metaData.Description);
    }

    [Fact]
    public void ToMetaData_BasicProtoMetaData_ConvertsCorrectly()
    {
        var protoMetaData = new ProtoMetaData
        {
            IsMultiPart = true,
            ContentType = "application/xml",
            Size = 2048,
            Seq = 100ul,
            FileName = "data.xml",
            FileType = "xml",
            Md5 = "xyz789pqr012",
            Description = "XML data description"
        };

        var metaData = protoMetaData.ToMetaData();

        Assert.NotNull(metaData);
        Assert.True(metaData.IsMultiPart);
        Assert.Equal("application/xml", metaData.ContentType);
        Assert.Equal(2048ul, metaData.Size);
        Assert.Equal(100, metaData.Seq);
        Assert.Equal("data.xml", metaData.FileName);
        Assert.Equal("xml", metaData.FileType);
        Assert.Equal("xyz789pqr012", metaData.Md5);
        Assert.Equal("XML data description", metaData.Description);
    }

    [Fact]
    public void MetaDataRoundTrip_PreservesData()
    {
        var originalMetaData = new MetaData
        {
            IsMultiPart = true,
            ContentType = "application/octet-stream",
            Size = 4096ul,
            Seq = 255,
            FileName = "binary.dat",
            FileType = "dat",
            Md5 = "hash123456",
            Description = "Round trip test data"
        };

        // Round trip: MetaData -> ProtoMetaData -> MetaData
        var protoMetaData = originalMetaData.ToProtoMetaData();
        var roundTripMetaData = protoMetaData.ToMetaData();

        Assert.NotNull(roundTripMetaData);
        Assert.Equal(originalMetaData.IsMultiPart, roundTripMetaData.IsMultiPart);
        Assert.Equal(originalMetaData.ContentType, roundTripMetaData.ContentType);
        Assert.Equal(originalMetaData.Size, roundTripMetaData.Size);
        Assert.Equal(originalMetaData.Seq, roundTripMetaData.Seq);
        Assert.Equal(originalMetaData.FileName, roundTripMetaData.FileName);
        Assert.Equal(originalMetaData.FileType, roundTripMetaData.FileType);
        Assert.Equal(originalMetaData.Md5, roundTripMetaData.Md5);
        Assert.Equal(originalMetaData.Description, roundTripMetaData.Description);
    }
}