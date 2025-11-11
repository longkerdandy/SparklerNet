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
    public void ToMetaData_NullProtoMetaData_ThrowsArgumentNullException()
    {
        ProtoMetaData protoMetaData = null!;
        Assert.Throws<ArgumentNullException>(() => protoMetaData.ToMetaData());
    }

    [Theory]
    [InlineData(false, "", 0ul, 0ul, "", "", "", "")] // Empty MetaData
    [InlineData(true, "application/json", 1024ul, 42ul, "test.json", "json", "abc123def456",
        "Test description")] // Basic MetaData
    public void ToProtoMetaData_ConvertsCorrectly(bool isMultiPart, string contentType, ulong size, ulong seq,
        string fileName, string fileType, string md5, string description)
    {
        var metaData = new MetaData
        {
            IsMultiPart = isMultiPart,
            ContentType = contentType,
            Size = size,
            Seq = seq,
            FileName = fileName,
            FileType = fileType,
            Md5 = md5,
            Description = description
        };

        var protoMetaData = metaData.ToProtoMetaData();

        Assert.NotNull(protoMetaData);
        Assert.Equal(isMultiPart, protoMetaData.IsMultiPart);
        Assert.Equal(contentType, protoMetaData.ContentType);
        Assert.Equal(size, protoMetaData.Size);
        Assert.Equal(seq, protoMetaData.Seq);
        Assert.Equal(fileName, protoMetaData.FileName);
        Assert.Equal(fileType, protoMetaData.FileType);
        Assert.Equal(md5, protoMetaData.Md5);
        Assert.Equal(description, protoMetaData.Description);
    }

    [Theory]
    [InlineData(false, "", 0ul, 0ul, "", "", "", "")] // Empty ProtoMetaData
    [InlineData(true, "application/xml", 2048ul, 100ul, "data.xml", "xml", "xyz789pqr012",
        "XML data description")] // Basic ProtoMetaData
    public void ToMetaData_ConvertsCorrectly(bool isMultiPart, string contentType, ulong size, ulong seq,
        string fileName, string fileType, string md5, string description)
    {
        var protoMetaData = new ProtoMetaData
        {
            IsMultiPart = isMultiPart,
            ContentType = contentType,
            Size = size,
            Seq = seq,
            FileName = fileName,
            FileType = fileType,
            Md5 = md5,
            Description = description
        };

        var metaData = protoMetaData.ToMetaData();

        Assert.NotNull(metaData);
        Assert.Equal(isMultiPart, metaData.IsMultiPart);
        Assert.Equal(contentType, metaData.ContentType);
        Assert.Equal(size, metaData.Size);
        Assert.Equal(seq, metaData.Seq);
        Assert.Equal(fileName, metaData.FileName);
        Assert.Equal(fileType, metaData.FileType);
        Assert.Equal(md5, metaData.Md5);
        Assert.Equal(description, metaData.Description);
    }

    [Theory]
    [InlineData(true, "application/octet-stream", 4096ul, 255ul, "binary.dat", "dat", "hash123456",
        "Round trip test data")]
    [InlineData(false, "text/plain", 100ul, 5ul, "example.txt", "txt", "plainhash", "Simple text file")]
    public void MetaDataRoundTrip_PreservesData(bool isMultiPart, string contentType, ulong size, ulong seq,
        string fileName, string fileType, string md5, string description)
    {
        var originalMetaData = new MetaData
        {
            IsMultiPart = isMultiPart,
            ContentType = contentType,
            Size = size,
            Seq = seq,
            FileName = fileName,
            FileType = fileType,
            Md5 = md5,
            Description = description
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