using MQTTnet;
using SparklerNet.Core.Options;

namespace SparklerNet.Samples.Profiles;

/// <summary>
///     This interface defines the contract for a Sparkplug client profile.
/// </summary>
public interface IProfile
{
    /// <summary>
    ///     Gets the MQTT client options for the profile.
    /// </summary>
    /// <returns>The MQTT client options.</returns>
    public MqttClientOptions GetMqttClientOptions();
    
    /// <summary>
    ///     Gets the Sparkplug client options for the profile.
    /// </summary>
    /// <returns>The Sparkplug client options.</returns>
    public SparkplugClientOptions GetSparkplugClientOptions();
}