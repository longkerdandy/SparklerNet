using JetBrains.Annotations;

namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug MetaData object is used to describe different types of binary data.
/// </summary>
[PublicAPI]
public record MetaData
{
    // The metric contains part of a multi-part message?
    public bool? IsMultiPart { get; init; }

    // The content type of the given metric value if applicable.
    public string? ContentType { get; init; }

    // The size of the metric value.
    public ulong? Size { get; init; }

    // The sequence number of this part of a multipart metric.
    public ulong? Seq { get; init; }

    // If this is a file metric, representing the file name.
    public string? FileName { get; init; }

    // If this is a file metric, representing the file type.
    public string? FileType { get; init; }

    // Byte array or file metric that have a md5sum.
    public string? Md5 { get; init; }

    // Representing any other pertinent metadata
    public string? Description { get; init; }
}