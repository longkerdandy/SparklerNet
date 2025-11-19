using MQTTnet;
using MQTTnet.Formatter;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Options;

namespace SparklerNet.Samples.Profiles;

/// <summary>
///     This is a Sparkplug Host Application profile that will connect to the local MQTT broker for Eclipse™ Sparkplug™ TCK
///     tests. For more information about Eclipse™ Sparkplug™ TCK, visit
///     <a href="https://github.com/eclipse-sparkplug/sparkplug/tree/master/tck">Eclipse™ Sparkplug™ TCK</a>
/// </summary>
public class TckApplicationProfile : IProfile
{
    /// <inheritdoc />
    public MqttClientOptions GetMqttClientOptions()
    {
        return new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .Build();
    }

    /// <inheritdoc />
    public SparkplugClientOptions GetSparkplugClientOptions()
    {
        return new SparkplugClientOptions
        {
            Version = SparkplugVersion.V300,
            HostApplicationId = "SparklerNetSimpleHostApp",
            AlwaysSubscribeToWildcardTopic = true,
            EnableMessageOrdering = true,
            SeqReorderTimeout = 5000
        };
    }
}