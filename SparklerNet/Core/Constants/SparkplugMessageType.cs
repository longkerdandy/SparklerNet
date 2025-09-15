using System.Diagnostics.CodeAnalysis;

namespace SparklerNet.Core.Constants;

/// <summary>
///     The Sparkplug message types.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum SparkplugMessageType
{
    NBIRTH,
    NDEATH,
    DBIRTH,
    DDEATH,
    NDATA,
    DDATA,
    NCMD,
    DCMD,
    STATE
}