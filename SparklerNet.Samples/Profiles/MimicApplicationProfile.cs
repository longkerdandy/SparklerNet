using MQTTnet;
using MQTTnet.Formatter;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Options;

namespace SparklerNet.Samples.Profiles;

/// <summary>
///     This is a Sparkplug Host Application profile that will subscribe to the MIMIC topic.
///     The MIMIC MQTT Simulators are shared, read-only Sparkplug B sensors publishing unique Sparkplug messages with
///     temperature telemetry to the public brokers TEST.MOSQUITTO.ORG and BROKER.HIVEMQ.COM. For more information about
///     MIMIC MQTT Simulators, visit <a href="https://mqttlab.iotsim.io/sparkplug/">MQTTLAB.IOTSIM.IO/sparkplug</a>
/// </summary>
public class MimicApplicationProfile : IProfile
{
    /// <inheritdoc />
    public MqttClientOptions GetMqttClientOptions()
    {
        return new MqttClientOptionsBuilder()
            .WithTcpServer("BROKER.HIVEMQ.COM", 1883)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .Build();
    }

    /// <inheritdoc />
    public SparkplugClientOptions GetSparkplugClientOptions()
    {
        return new SparkplugClientOptions
        {
            Version = SparkplugVersion.V300,
            HostApplicationId = "SparklerNetSimpleHostApp",
            Subscriptions = { new MqttTopicFilterBuilder().WithTopic("spBv1.0/MIMIC/#").WithAtLeastOnceQoS().Build() },
            EnableMessageOrdering = true
        };
    }
}