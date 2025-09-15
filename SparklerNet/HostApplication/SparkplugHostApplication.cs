using System.Text.Json;
using JetBrains.Annotations;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using SparklerNet.Core.Model;
using SparklerNet.Core.Options;
using SparklerNet.Core.Topics;

namespace SparklerNet.HostApplication;

/// <summary>
///     A Sparkplug Host Application is typically at a central location and primarily receives data from
///     multiple Sparkplug Edge Nodes. It also may send command messages to Sparkplug Edge Nodes to write
///     to outputs of Sparkplug Edge Nodes and/or Devices. Sparkplug Host Applications may also send
///     rebirth requests to Edge Nodes when required.
/// </summary>
public class SparkplugHostApplication
{
    private readonly MqttClientOptions _mqttOptions;
    private readonly SparkplugClientOptions _sparkplugOptions;

    // MQTT Client
    [UsedImplicitly] protected readonly IMqttClient MqttClient;

    public SparkplugHostApplication(MqttClientOptions mqttOptions, SparkplugClientOptions sparkplugOptions)
    {
        // Validations
        ArgumentNullException.ThrowIfNull(sparkplugOptions.HostApplicationId);

        _mqttOptions = mqttOptions;
        _sparkplugOptions = sparkplugOptions;

        // Create a new MQTT client.
        var factory = new MqttClientFactory();
        MqttClient = factory.CreateMqttClient();
    }

    /// <summary>
    ///     Start the Sparkplug Host Application.
    ///     Initialization will be performed in accordance with the Sparkplug specification.
    /// </summary>
    public async Task Start()
    {
        // The Sparkplug start up process:
        // 1. Connect to the MQTT Broker and use the Will Message.
        // 2. Subscribe to the MQTT topics.
        // 3. Publish the STATE(Birth Certificate) message.
        // The timestamp value MUST be the same value set in the previous MQTT CONNECT packet’s Will Message payload.
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await ConnectAsync(timestamp);
        await SubscribeAsync();
        await PublishStateMessageAsync(true, timestamp);
    }

    /// <summary>
    ///     Stop the Sparkplug Host Application.
    ///     Termination will be performed in accordance with the Sparkplug specification.
    /// </summary>
    public async Task StopAsync()
    {
        // Publish the STATE(Death Certificate) message.
        await PublishStateMessageAsync(false, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        await MqttClient.DisconnectAsync();
    }

    /// <summary>
    ///     Connect to the MQTT Broker and use the Will Message/Settings compliant with the Sparkplug specification.
    ///     The Sparkplug specification require using a v3.1.1 or v5.0 compliant MQTT Client.
    /// </summary>
    [UsedImplicitly]
    protected async Task<MqttClientConnectResult> ConnectAsync(long timestamp)
    {
        // The CONNECT Control Packet for all Sparkplug Host Applications when using MQTT 3.1.1 MUST set the MQTT 
        // Clean Session flag to true.
        // The CONNECT Control Packet for all Sparkplug Host Applications when using MQTT 5.0 MUST set the MQTT 
        // Clean Start flag to true and the Session Expiry Interval to 0.
        switch (_mqttOptions.ProtocolVersion)
        {
            case MqttProtocolVersion.V311:
                _mqttOptions.CleanSession = true;
                break;
            case MqttProtocolVersion.V500:
                _mqttOptions.CleanSession = true; // Same as Clean Start
                _mqttOptions.SessionExpiryInterval = 0;
                break;
            case MqttProtocolVersion.Unknown:
            case MqttProtocolVersion.V310:
            default:
                throw new ArgumentOutOfRangeException(nameof(_mqttOptions.ProtocolVersion),
                    _mqttOptions.ProtocolVersion,
                    "Unsupported MQTT protocol version");
        }

        // When the Sparkplug Host Application MQTT client establishes an MQTT session to the MQTT Server(s), the
        // Death Certificate will be part of the Will Topic and Will Payload registered in the MQTT CONNECT packet.
        // The MQTT Quality of Service (QoS) MUST be set to 1.
        // The MQTT retain flag for the Death Certificate MUST be set to TRUE.
        _mqttOptions.WillTopic = SparkplugTopicFactory.CreateHostApplicationStateTopic(
            _sparkplugOptions.Version, _sparkplugOptions.HostApplicationId!);
        _mqttOptions.WillPayload =
            JsonSerializer.SerializeToUtf8Bytes(new StatePayload { Online = false, Timestamp = timestamp });
        _mqttOptions.WillQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;
        _mqttOptions.WillRetain = true;

        // Connect to MQTT Broker.
        return await MqttClient.ConnectAsync(_mqttOptions);
    }

    /// <summary>
    ///     Subscribe to the MQTT topics based on the 'Subscriptions' option.
    ///     By default, the Sparkplug Host Application will subscribe to Sparkplug wildcard topic (e.g. spBv1.0/#) and the Host
    ///     Application's self STATE topic (e.g. spBv1.0/STATE/sparkplug_host_id).
    ///     However, it may also make sense for a Host Application to subscribe only to a specific Sparkplug Group. For example
    ///     subscribing to 'spBv1.0/Group1/' is also valid. A Host Application could even issue a subscription to subscribe to
    ///     only a single Sparkplug Edge Node using this: 'spBv1.0/Group1/+/EdgeNode1/#'. A Sparkplug Host Application could
    ///     subscribe to a combination of specific Sparkplug Groups and/or Edge Nodes as well.
    /// </summary>
    [UsedImplicitly]
    protected async Task SubscribeAsync()
    {
        // Remove the self (STATE) subscription if present.
        var stateTopic = SparkplugTopicFactory.CreateHostApplicationStateTopic(
            _sparkplugOptions.Version, _sparkplugOptions.HostApplicationId!);
        _sparkplugOptions.Subscriptions.RemoveAll(topicFilter => topicFilter.Topic == stateTopic);

        // Add the default Sparkplug wildcard subscription if the subscriptions option is empty.
        if (_sparkplugOptions.Subscriptions.Count == 0)
        {
            var spBTopic = SparkplugTopicFactory.CreateSparkplugWildcardTopic(_sparkplugOptions.Version);
            _sparkplugOptions.Subscriptions.Add(
                new MqttTopicFilterBuilder().WithTopic(spBTopic).WithAtLeastOnceQoS().Build());
        }

        // Add the self (STATE) subscription.
        _sparkplugOptions.Subscriptions.Add(
            new MqttTopicFilterBuilder().WithTopic(stateTopic).WithAtLeastOnceQoS().Build());

        // Loop and subscribe to each topic.
        foreach (var topic in _sparkplugOptions.Subscriptions) await MqttClient.SubscribeAsync(topic);
    }

    /// <summary>
    ///     Publish the Sparkplug STATE message to the MQTT broker.
    ///     Once an MQTT Session has been established,  the Sparkplug Host Application MUST publish a new STATE message.
    /// </summary>
    [UsedImplicitly]
    protected async Task<MqttClientPublishResult> PublishStateMessageAsync(bool online, long timestamp)
    {
        // The MQTT Quality of Service (QoS) MUST be set to 1.
        // The MQTT retain flag for the Birth Certificate MUST be set to TRUE.
        var stateMessage = new MqttApplicationMessageBuilder()
            .WithTopic(SparkplugTopicFactory.CreateHostApplicationStateTopic(
                _sparkplugOptions.Version, _sparkplugOptions.HostApplicationId!))
            .WithPayload(
                JsonSerializer.SerializeToUtf8Bytes(new StatePayload { Online = online, Timestamp = timestamp }))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();

        // Publish the message
        return await MqttClient.PublishAsync(stateMessage);
    }
}