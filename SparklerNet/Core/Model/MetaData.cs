namespace SparklerNet.Core.Model;

/// <summary>
///     A Sparkplug MetaData object is used to describe different types of binary data.
/// </summary>
public record MetaData
{
    /// <summary>
    ///     Whether the metric contains part of a multipart message.
    /// </summary>
    public bool? IsMultiPart { get; init; }

    /// <summary>
    ///     The content type of the given metric value if applicable.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    ///     The size of the metric value.
    /// </summary>
    public ulong? Size { get; init; }

    /// <summary>
    ///     The sequence number of this part of a multipart metric.
    /// </summary>
    public ulong? Seq { get; init; }

    /// <summary>
    ///     If this is a file metric, representing the file name.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    ///     If this is a file metric, representing the file type.
    /// </summary>
    public string? FileType { get; init; }

    /// <summary>
    ///     Byte array or file metric that have a md5sum.
    /// </summary>
    public string? Md5 { get; init; }

    /// <summary>
    ///     Representing any other pertinent metadata
    /// </summary>
    public string? Description { get; init; }
}