using System.Text.Json.Serialization;

namespace SparklerNet.Core.Model;

/// <summary>
///     The Sparkplug STATE message payload.
/// </summary>
public sealed record StatePayload
{
    //  Is online?
    [JsonPropertyName("online")] public bool Online { get; init; }

    // Milliseconds since Epoch
    [JsonPropertyName("timestamp")] public long Timestamp { get; init; }
}