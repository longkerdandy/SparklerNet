using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Events;
using SparklerNet.Core.Extensions;
using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using SparklerNet.Core.Options;
using SparklerNet.Core.Topics;
using static SparklerNet.Core.Constants.SparkplugMessageType;
using ProtoPayload = SparklerNet.Core.Protobuf.Payload;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable MemberCanBePrivate.Global

namespace SparklerNet.HostApplication;

/// <summary>
///     A Sparkplug Host Application is typically at a central location and primarily receives data from
///     multiple Sparkplug Edge Nodes. It also may send command messages to Sparkplug Edge Nodes to write
///     to outputs of Sparkplug Edge Nodes and/or Devices. Sparkplug Host Applications may also send
///     rebirth requests to Edge Nodes when required.
/// </summary>
public class SparkplugHostApplication
{
    private readonly SparkplugMessageEvents _events = new();
    private readonly ILogger<SparkplugHostApplication> _logger;
    private readonly MqttClientOptions _mqttOptions;
    private readonly SparkplugClientOptions _sparkplugOptions;

    /// <summary>
    ///     Create a new instance of the Sparkplug Host Application.
    /// </summary>
    /// <param name="mqttOptions">The MQTT Client Options.</param>
    /// <param name="sparkplugOptions">The Sparkplug Client Options.</param>
    /// <param name="logger">The Logger.</param>
    public SparkplugHostApplication(MqttClientOptions mqttOptions, SparkplugClientOptions sparkplugOptions,
        ILogger<SparkplugHostApplication> logger)
    {
        // Validate HostApplicationId
        SparkplugNamespace.ValidateNamespaceElement(sparkplugOptions.HostApplicationId,
            nameof(sparkplugOptions.HostApplicationId));

        _mqttOptions = mqttOptions;
        _sparkplugOptions = sparkplugOptions;
        _logger = logger;

        // Create a new MQTT client.
        var factory = new MqttClientFactory();
        MqttClient = factory.CreateMqttClient();

        // Subscribe to the ApplicationMessageReceived event to process incoming messages
        MqttClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
        MqttClient.DisconnectedAsync += HandleDisconnectedAsync;
    }

    // MQTT Client
    public IMqttClient MqttClient { get; }

    public event Func<EdgeNodeMessageEventArgs, Task> EdgeNodeBirthReceivedAsync
    {
        add => _events.EdgeNodeBirthReceivedEvent.AddHandler(value);
        remove => _events.EdgeNodeBirthReceivedEvent.RemoveHandler(value);
    }

    public event Func<EdgeNodeMessageEventArgs, Task> EdgeNodeDeathReceivedAsync
    {
        add => _events.EdgeNodeDeathReceivedEvent.AddHandler(value);
        remove => _events.EdgeNodeDeathReceivedEvent.RemoveHandler(value);
    }

    public event Func<EdgeNodeMessageEventArgs, Task> EdgeNodeDataReceivedAsync
    {
        add => _events.EdgeNodeDataReceivedEvent.AddHandler(value);
        remove => _events.EdgeNodeDataReceivedEvent.RemoveHandler(value);
    }

    public event Func<DeviceMessageEventArgs, Task> DeviceBirthReceivedAsync
    {
        add => _events.DeviceBirthReceivedEvent.AddHandler(value);
        remove => _events.DeviceBirthReceivedEvent.RemoveHandler(value);
    }

    public event Func<DeviceMessageEventArgs, Task> DeviceDeathReceivedAsync
    {
        add => _events.DeviceDeathReceivedEvent.AddHandler(value);
        remove => _events.DeviceDeathReceivedEvent.RemoveHandler(value);
    }

    public event Func<DeviceMessageEventArgs, Task> DeviceDataReceivedAsync
    {
        add => _events.DeviceDataReceivedEvent.AddHandler(value);
        remove => _events.DeviceDataReceivedEvent.RemoveHandler(value);
    }

    public event Func<HostApplicationMessageEventArgs, Task> StateReceivedAsync
    {
        add => _events.StateReceivedEvent.AddHandler(value);
        remove => _events.StateReceivedEvent.RemoveHandler(value);
    }

    public event Func<ConnectedEventArgs, Task> ConnectedReceivedAsync
    {
        add => _events.ConnectedReceivedEvent.AddHandler(value);
        remove => _events.ConnectedReceivedEvent.RemoveHandler(value);
    }

    public event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedReceivedAsync
    {
        add => _events.DisconnectedReceivedEvent.AddHandler(value);
        remove => _events.DisconnectedReceivedEvent.RemoveHandler(value);
    }

    public event Func<MqttApplicationMessageReceivedEventArgs, Task> UnsupportedReceivedAsync
    {
        add => _events.UnsupportedReceivedEvent.AddHandler(value);
        remove => _events.UnsupportedReceivedEvent.RemoveHandler(value);
    }

    /// <summary>
    ///     Start the Sparkplug Host Application.
    ///     Initialization will be performed in accordance with the Sparkplug specification.
    /// </summary>
    public async Task<(MqttClientConnectResult connectResult, MqttClientSubscribeResult? subscribeResult)> StartAsync()
    {
        _logger.LogInformation("Starting Sparkplug Host Application {HostApplicationId}",
            _sparkplugOptions.HostApplicationId);

        // The Sparkplug start up process:
        // 1. Connect to the MQTT Broker and use the Will Message.
        // 2. Subscribe to the MQTT topics.
        // 3. Publish the STATE(Birth Certificate) message.
        // The timestamp value MUST be the same value set in the previous MQTT CONNECT packet's Will Message payload.
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var connectResult = await ConnectAsync(timestamp);

        // If the connection failed, return the connect result with null subscribe result.
        // This will trigger the DisconnectedReceivedAsync event.
        if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
        {
            _logger.LogWarning(
                "Failed to start Sparkplug Host Application, unable to connect to MQTT Broker with result {ResultCode}",
                connectResult.ResultCode);
            return (connectResult, null);
        }

        // If the connection is successful, subscribe to the MQTT topics and raise the ConnectedReceivedEvent event.
        var subscribeResult = await SubscribeAsync();
        await PublishStateMessageAsync(true, timestamp);
        await _events.ConnectedReceivedEvent.InvokeAsync(new ConnectedEventArgs(connectResult, subscribeResult));

        _logger.LogInformation("Successfully started Sparkplug Host Application {HostApplicationId}.",
            _sparkplugOptions.HostApplicationId);
        return (connectResult, subscribeResult);
    }

    /// <summary>
    ///     Stop the Sparkplug Host Application.
    ///     Termination will be performed in accordance with the Sparkplug specification.
    /// </summary>
    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping Sparkplug Host Application {HostApplicationId}",
            _sparkplugOptions.HostApplicationId);

        // Publish the STATE(Death Certificate) message.
        await PublishStateMessageAsync(false, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        await MqttClient.DisconnectAsync();

        _logger.LogInformation("Successfully stopped Sparkplug Host Application {HostApplicationId}.",
            _sparkplugOptions.HostApplicationId);
    }

    /// <summary>
    ///     Connect to the MQTT Broker and use the Will Message/Settings compliant with the Sparkplug specification.
    ///     The Sparkplug specification require using a v3.1.1 or v5.0 compliant MQTT Client.
    /// </summary>
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
                _logger.LogError("Trying to connect to MQTT Broker with unsupported protocol version {ProtocolVersion}",
                    _mqttOptions.ProtocolVersion);
                throw new NotSupportedException($"Unsupported MQTT protocol version: {_mqttOptions.ProtocolVersion}");
        }

        // When the Sparkplug Host Application MQTT client establishes an MQTT session to the MQTT Server(s), the
        // Death Certificate will be part of the Will Topic and Will Payload registered in the MQTT CONNECT packet.
        // The MQTT Quality of Service (QoS) MUST be set to 1.
        // The MQTT retain flag for the Death Certificate MUST be set to TRUE.
        _mqttOptions.WillTopic = SparkplugTopicFactory.CreateStateTopic(
            _sparkplugOptions.Version, _sparkplugOptions.HostApplicationId);
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
    protected async Task<MqttClientSubscribeResult> SubscribeAsync()
    {
        // Remove the self (STATE) subscription if present.
        var stateTopic = SparkplugTopicFactory.CreateStateTopic(
            _sparkplugOptions.Version, _sparkplugOptions.HostApplicationId);
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

        // Add the subscriptions to the subscribe options.
        var optionsBuilder = new MqttClientSubscribeOptionsBuilder();
        foreach (var subscription in _sparkplugOptions.Subscriptions) optionsBuilder.WithTopicFilter(subscription);
        var result = await MqttClient.SubscribeAsync(optionsBuilder.Build());

        _logger.LogInformation("Subscribing to MQTT Broker with Topics: {Result}", result.ToFormattedString());

        return result;
    }

    /// <summary>
    ///     Publish the Sparkplug STATE message to the MQTT broker.
    ///     Once an MQTT Session has been established,  the Sparkplug Host Application MUST publish a new STATE message.
    /// </summary>
    protected async Task<MqttClientPublishResult> PublishStateMessageAsync(bool online, long timestamp)
    {
        // The MQTT Quality of Service (QoS) MUST be set to 1.
        // The MQTT retain flag for the Birth Certificate MUST be set to TRUE.
        var stateMessage = new MqttApplicationMessageBuilder()
            .WithTopic(SparkplugTopicFactory.CreateStateTopic(
                _sparkplugOptions.Version, _sparkplugOptions.HostApplicationId))
            .WithPayload(
                JsonSerializer.SerializeToUtf8Bytes(new StatePayload { Online = online, Timestamp = timestamp }))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();

        return await PublishMessageAsync(stateMessage, "STATE");
    }

    /// <summary>
    ///     Publish the Sparkplug NCMD message to the MQTT broker.
    /// </summary>
    /// <param name="groupId">The Sparkplug Group ID.</param>
    /// <param name="edgeNodeId">The Sparkplug Edge Node ID.</param>
    /// <param name="payload">The Payload to publish.</param>
    /// <returns>The MQTT Client Publish Result.</returns>
    public async Task<MqttClientPublishResult> PublishEdgeNodeCommandMessageAsync(string groupId, string edgeNodeId,
        Payload payload)
    {
        // Validate the group ID, edge node ID and the payload.
        SparkplugNamespace.ValidateNamespaceElement(groupId, nameof(groupId));
        SparkplugNamespace.ValidateNamespaceElement(edgeNodeId, nameof(edgeNodeId));
        ArgumentNullException.ThrowIfNull(payload);

        // NCMD messages MUST be published with MQTT QoS equal to 0 and retain equal to false.
        var ncmdMessage = new MqttApplicationMessageBuilder()
            .WithTopic(SparkplugTopicFactory.CreateEdgeNodeTopic(_sparkplugOptions.Version, groupId,
                NCMD, edgeNodeId))
            .WithPayload(payload.ToProtoPayload().ToByteArray())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .WithRetainFlag(false)
            .Build();

        return await PublishMessageAsync(ncmdMessage, "NCMD");
    }

    /// <summary>
    ///     Publish the Sparkplug DCMD message to the MQTT broker.
    /// </summary>
    /// <param name="groupId">The Sparkplug Group ID.</param>
    /// <param name="edgeNodeId">The Sparkplug Edge Node ID.</param>
    /// <param name="deviceId">The Sparkplug Device ID.</param>
    /// <param name="payload">The Payload to publish.</param>
    /// <returns>The MQTT Client Publish Result.</returns>
    public async Task<MqttClientPublishResult> PublishDeviceCommandMessageAsync(string groupId, string edgeNodeId,
        string deviceId, Payload payload)
    {
        // Validate the group ID, edge node ID, device ID and the payload.
        SparkplugNamespace.ValidateNamespaceElement(groupId, nameof(groupId));
        SparkplugNamespace.ValidateNamespaceElement(edgeNodeId, nameof(edgeNodeId));
        SparkplugNamespace.ValidateNamespaceElement(deviceId, nameof(deviceId));
        ArgumentNullException.ThrowIfNull(payload);

        // DCMD messages MUST be published with MQTT QoS equal to 0 and retain equal to false.
        var dcmdMessage = new MqttApplicationMessageBuilder()
            .WithTopic(SparkplugTopicFactory.CreateDeviceTopic(_sparkplugOptions.Version, groupId,
                DCMD, edgeNodeId, deviceId))
            .WithPayload(payload.ToProtoPayload().ToByteArray())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .WithRetainFlag(false)
            .Build();

        return await PublishMessageAsync(dcmdMessage, "DCMD");
    }

    /// <summary>
    ///     Publishes a MQTT message and logs the result.
    /// </summary>
    /// <param name="message">The MQTT message to publish.</param>
    /// <param name="messageType">The type of message being published (for logging purposes).</param>
    /// <returns>The MQTT Client Publish Result.</returns>
    protected async Task<MqttClientPublishResult> PublishMessageAsync(MqttApplicationMessage message,
        string messageType)
    {
        var result = await MqttClient.PublishAsync(message);
        if (!result.IsSuccess)
            _logger.LogWarning("Failed to publish {MessageType} message to topic {Topic} with result {ResultCode}",
                messageType, message.Topic, result.ReasonCode);
        else
            _logger.LogInformation(
                "Successfully published {MessageType} message to topic {Topic} with result {ResultCode}", messageType,
                message.Topic, result.ReasonCode);
        return result;
    }

    /// <summary>
    ///     Handles incoming MQTT messages and triggers appropriate events based on message type.
    ///     Unsupported message types will be published to the UnsupportedReceived event.
    /// </summary>
    protected async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        try
        {
            // Parse the topic to determine the message type, throw exception if invalid
            var topic = eventArgs.ApplicationMessage.Topic;
            var (version, groupId, messageType, edgeNodeId, deviceId, hostId) = SparkplugTopicParser.ParseTopic(topic);

            // Validate the payload length, throw exception if invalid
            if (eventArgs.ApplicationMessage.Payload.IsEmpty)
                throw new NotSupportedException($"Invalid payload length for topic {topic}.");

            _logger.LogInformation("Received {MessageType} message from topic {Topic}", messageType, topic);

            if (messageType == STATE)
            {
                // Parse the payload as STATE message and raise the event
                var statePayload = StatePayloadConverter.DeserializeStatePayload(eventArgs.ApplicationMessage.Payload);
                await _events.StateReceivedEvent.InvokeAsync(
                    new HostApplicationMessageEventArgs(version, messageType, hostId!, statePayload, eventArgs));
            }
            else
            {
                // Parse the payload as regular message
                var protoPayload = ProtoPayload.Parser.ParseFrom(eventArgs.ApplicationMessage.Payload);
                var payload = protoPayload.ToPayload();

                // Raise the appropriate event based on message type
                await (messageType switch
                {
                    NBIRTH => _events.EdgeNodeBirthReceivedEvent.InvokeAsync(
                        new EdgeNodeMessageEventArgs(version, messageType, groupId!, edgeNodeId!, payload, eventArgs)),
                    NDEATH => _events.EdgeNodeDeathReceivedEvent.InvokeAsync(
                        new EdgeNodeMessageEventArgs(version, messageType, groupId!, edgeNodeId!, payload, eventArgs)),
                    NDATA => _events.EdgeNodeDataReceivedEvent.InvokeAsync(
                        new EdgeNodeMessageEventArgs(version, messageType, groupId!, edgeNodeId!, payload, eventArgs)),
                    DBIRTH => _events.DeviceBirthReceivedEvent.InvokeAsync(new DeviceMessageEventArgs(version,
                        messageType, groupId!, edgeNodeId!, deviceId!, payload, eventArgs)),
                    DDEATH => _events.DeviceDeathReceivedEvent.InvokeAsync(new DeviceMessageEventArgs(version,
                        messageType, groupId!, edgeNodeId!, deviceId!, payload, eventArgs)),
                    DDATA => _events.DeviceDataReceivedEvent.InvokeAsync(new DeviceMessageEventArgs(version,
                        messageType, groupId!, edgeNodeId!, deviceId!, payload, eventArgs)),
                    NCMD or DCMD => Task.CompletedTask,
                    _ => throw new NotSupportedException(
                        $"Not supported Sparkplug message type {messageType} for Host Application.")
                });
            }
        }
        catch (Exception)
        {
            // Raise the UnsupportedReceived event
            await _events.UnsupportedReceivedEvent.InvokeAsync(eventArgs);
        }
    }

    /// <summary>
    ///     Handles MQTT client disconnected events and triggers the DisconnectedReceived event.
    /// </summary>
    /// <param name="eventArgs">The MQTT client disconnected event arguments.</param>
    protected async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
    {
        if (eventArgs.Reason == MqttClientDisconnectReason.NormalDisconnection)
        {
            _logger.LogInformation("MQTT client disconnected normally.");
        }
        else
        {
            _logger.LogInformation("MQTT client disconnected unexpectedly with reason {Reason}", eventArgs.Reason);

            // Raise the DisconnectedReceived event
            await _events.DisconnectedReceivedEvent.InvokeAsync(eventArgs);
        }
    }
}