using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SparklerNet.Core.Model;

/// <summary>
///     The Sparkplug STATE message payload.
/// </summary>
[PublicAPI]
public sealed record StatePayload
{
    //  Is online?
    [JsonPropertyName("online")] public bool Online { get; init; }

    // Milliseconds since Epoch
    [JsonPropertyName("timestamp")] public long Timestamp { get; init; }
}