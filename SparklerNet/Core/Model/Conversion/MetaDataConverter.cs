using JetBrains.Annotations;
using ProtoMetaData = SparklerNet.Core.Protobuf.Payload.Types.MetaData;

namespace SparklerNet.Core.Model.Conversion;

/// <summary>
///     Converts between <see cref="MetaData" /> and <see cref="ProtoMetaData" />.
/// </summary>
[PublicAPI]
public static class MetaDataConverter
{
    /// <summary>
    ///     Converts a <see cref="MetaData" /> to a Protobuf <see cref="ProtoMetaData" />.
    /// </summary>
    /// <param name="metaData">The MetaData to convert.</param>
    /// <returns>The converted Protobuf MetaData.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metaData" /> is null.</exception>
    public static ProtoMetaData ToProtoMetaData(this MetaData metaData)
    {
        ArgumentNullException.ThrowIfNull(metaData);

        // Map basic metadata properties
        var protoMetaData = new ProtoMetaData();
        if (metaData.IsMultiPart.HasValue) protoMetaData.IsMultiPart = metaData.IsMultiPart.Value;
        if (!string.IsNullOrEmpty(metaData.ContentType)) protoMetaData.ContentType = metaData.ContentType;
        if (metaData.Size.HasValue) protoMetaData.Size = metaData.Size.Value;
        if (metaData.Seq.HasValue) protoMetaData.Seq = (ulong)metaData.Seq.Value;
        if (!string.IsNullOrEmpty(metaData.FileName)) protoMetaData.FileName = metaData.FileName;
        if (!string.IsNullOrEmpty(metaData.FileType)) protoMetaData.FileType = metaData.FileType;
        if (!string.IsNullOrEmpty(metaData.Md5)) protoMetaData.Md5 = metaData.Md5;
        if (!string.IsNullOrEmpty(metaData.Description)) protoMetaData.Description = metaData.Description;

        return protoMetaData;
    }

    /// <summary>
    ///     Converts a Protobuf <see cref="ProtoMetaData" /> to a <see cref="MetaData" />.
    /// </summary>
    /// <param name="protoMetaData">The Protobuf MetaData to convert.</param>
    /// <returns>The converted MetaData.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protoMetaData" /> is null.</exception>
    public static MetaData ToMetaData(this ProtoMetaData protoMetaData)
    {
        ArgumentNullException.ThrowIfNull(protoMetaData);

        // Create a new MetaData with all the properties from the protoMetaData
        var metaData = new MetaData
        {
            IsMultiPart = protoMetaData.IsMultiPart, // Defaults to false if not set
            ContentType = protoMetaData.ContentType, // Will be empty string if not set
            Size = protoMetaData.Size, // Defaults to 0 if not set
            Seq = (long)protoMetaData.Seq, // Defaults to 0 if not set
            FileName = protoMetaData.FileName, // Will be empty string if not set
            FileType = protoMetaData.FileType, // Will be empty string if not set
            Md5 = protoMetaData.Md5, // Will be empty string if not set
            Description = protoMetaData.Description // Will be empty string if not set
        };

        return metaData;
    }
}