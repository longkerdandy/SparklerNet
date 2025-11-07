using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
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
using SparklerNet.HostApplication.Caches;
using SparklerNet.HostApplication.Extensions;
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

    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    private readonly IMessageOrderingService _msgOrderingService;

    private readonly SparkplugClientOptions _sparkplugOptions;

    /// <summary>
    ///     Create a new instance of the Sparkplug Host Application.
    /// </summary>
    /// <param name="mqttOptions">The MQTT Client Options.</param>
    /// <param name="sparkplugOptions">The Sparkplug Client Options.</param>
    /// <param name="memoryCache">The Memory Cache.</param>
    /// <param name="logger">The Logger.</param>
    public SparkplugHostApplication(MqttClientOptions mqttOptions, SparkplugClientOptions sparkplugOptions,
        IMemoryCache memoryCache, ILogger<SparkplugHostApplication> logger)
    {
        // Validate HostApplicationId
        SparkplugNamespace.ValidateNamespaceElement(sparkplugOptions.HostApplicationId,
            nameof(sparkplugOptions.HostApplicationId));

        _mqttOptions = mqttOptions;
        _sparkplugOptions = sparkplugOptions;
        _logger = logger;
        _msgOrderingService = new MessageOrderingService(memoryCache, _sparkplugOptions);

        // Create a new MQTT client.
        var factory = new MqttClientFactory();
        MqttClient = factory.CreateMqttClient();

        // Subscribe to the ApplicationMessageReceived event to process incoming messages
        MqttClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
        MqttClient.DisconnectedAsync += HandleDisconnectedAsync;

        // Set the rebirth message request delegate
        _msgOrderingService.OnRebirthRequested = HandleRebirthRequested;

        // Set the pending messages processed delegate 
        _msgOrderingService.OnPendingMessages = HandlePendingMessages;
    }

    // MQTT Client
    public IMqttClient MqttClient { get; }

    /// <summary>
    ///     Handles rebirth request callbacks from IMessageOrderingService
    /// </summary>
    /// <param name="groupId">The group ID of the entity requiring rebirth</param>
    /// <param name="edgeNodeId">The edge node ID of the entity requiring rebirth</param>
    /// <param name="deviceId">The device ID of the entity requiring rebirth (optional)</param>
    private async Task HandleRebirthRequested(string groupId, string edgeNodeId, string? deviceId = null)
    {
        try
        {
            if (deviceId != null) await this.SendDeviceRebirthCommandAsync(groupId, edgeNodeId, deviceId);
            else await this.SendEdgeNodeRebirthCommandAsync(groupId, edgeNodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred while sending rebirth command. GroupId: {GroupId}, EdgeNodeId: {EdgeNodeId}, DeviceId: {DeviceId}",
                groupId, edgeNodeId, deviceId);
        }
    }

    /// <summary>
    ///     Processes data messages (NDATA and DDATA) and invokes the appropriate event.
    ///     When the ProcessDisorderedMessages option is enabled, pending messages will be processed in the order they were
    ///     received.
    /// </summary>
    /// <param name="messageContexts">The collection of NDATA and DDATA messages to process.</param>
    private async Task HandlePendingMessages(IEnumerable<SparkplugMessageEventArgs> messageContexts)
    {
        foreach (var messageContext in messageContexts)
            try
            {
                // Raise the message received event.
                await (messageContext.MessageType switch
                {
                    NDATA => _events.EdgeNodeDataReceivedEvent.InvokeAsync(messageContext),
                    DDATA => _events.DeviceDataReceivedEvent.InvokeAsync(messageContext),
                    _ => Task.CompletedTask // Other message types are ignored
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Exception occurred while handling message received event. GroupId: {GroupId}, EdgeNodeId: {EdgeNodeId}, DeviceId: {DeviceId}",
                    messageContext.GroupId, messageContext.EdgeNodeId, messageContext.DeviceId);
            }
    }

    /// <summary>
    ///     Start the Sparkplug Host Application.
    ///     Initialization will be performed in accordance with the Sparkplug specification.
    /// </summary>
    /// <returns>The task result contains the MQTT connect result and subscribe result.</returns>
    public async Task<(MqttClientConnectResult connectResult, MqttClientSubscribeResult? subscribeResult)> StartAsync()
    {
        _logger.LogInformation("Starting Sparkplug Host Application {HostApplicationId}",
            _sparkplugOptions.HostApplicationId);

        // The Sparkplug startup process:
        // 1. Connect to the MQTT Broker and use the Will Message.
        // 2. Subscribe to the MQTT topics.
        // 3. Publish the STATE(Birth Certificate) message.
        // The timestamp value MUST be the same value set in the previous MQTT CONNECT packet's Will Message payload.
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var connectResult = await ConnectAsync(timestamp);

        // If the connection failed, return the connected result with the subscribed result as null.
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
        try
        {
            // Raise the ConnectedReceivedEvent event.
            await _events.ConnectedReceivedEvent.InvokeAsync(new ConnectedEventArgs(connectResult, subscribeResult));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception occurred while handling client connected event");
        }

        _logger.LogInformation("Successfully started Sparkplug Host Application {HostApplicationId}.",
            _sparkplugOptions.HostApplicationId);
        return (connectResult, subscribeResult);
    }

    /// <summary>
    ///     Stop the Sparkplug Host Application.
    ///     Termination will be performed in accordance with the Sparkplug specification.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
    ///     The Sparkplug specification requires using a v3.1.1 or v5.0 compliant MQTT Client.
    /// </summary>
    /// <param name="timestamp">The timestamp to use in the Will Message.</param>
    /// <returns>The task result contains the MQTT connect result.</returns>
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
    ///     By default, the Sparkplug Host Application will subscribe to Sparkplug wildcard topic (e.g., spBv1.0/#) and the
    ///     Host
    ///     Application self's STATE topic (e.g., spBv1.0/STATE/sparkplug_host_id).
    ///     However, it may also make sense for a Host Application to subscribe only to a specific Sparkplug Group. For
    ///     example,
    ///     subscribing to 'spBv1.0/Group1/' is also valid. A Host Application could even issue a subscription to subscribe to
    ///     only a single Sparkplug Edge Node using this: 'spBv1.0/Group1/+/EdgeNode1/#'. A Sparkplug Host Application could
    ///     subscribe to a combination of specific Sparkplug Groups and/or Edge Nodes as well.
    /// </summary>
    /// <returns>The task result contains the MQTT subscribe result.</returns>
    protected async Task<MqttClientSubscribeResult> SubscribeAsync()
    {
        // Remove the self (STATE) subscription if present.
        var stateTopic = SparkplugTopicFactory.CreateStateTopic(
            _sparkplugOptions.Version, _sparkplugOptions.HostApplicationId);
        _sparkplugOptions.Subscriptions.RemoveAll(topicFilter => topicFilter.Topic == stateTopic);

        // Add the default Sparkplug wildcard subscription if the subscriptions' option is empty.
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
    /// <param name="online">The online status to publish.</param>
    /// <param name="timestamp">The timestamp to publish.</param>
    /// <returns>The task result contains the MQTT publish result.</returns>
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
    /// <returns>The task result contains the MQTT publish result.</returns>
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
    /// <returns>The task result contains the MQTT publish result.</returns>
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
    /// <returns>The task result contains the MQTT publish result.</returns>
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
    ///     Handles incoming MQTT messages and triggers appropriate events based on the message type.
    ///     Unsupported message types will be published to the UnsupportedReceived event.
    /// </summary>
    /// <param name="eventArgs">The event arguments containing the received MQTT message.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

            // Parse the payload as a STATE message
            if (messageType == STATE)
            {
                var statePayload = StatePayloadConverter.DeserializeStatePayload(eventArgs.ApplicationMessage.Payload);
                try
                {
                    await _events.StateReceivedEvent.InvokeAsync(
                        new HostApplicationMessageEventArgs(version, messageType, hostId!, statePayload, eventArgs));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception occurred while handling STATE message received event");
                }

                return; // return early
            }

            // Parse the payload as a regular message
            var protoPayload = ProtoPayload.Parser.ParseFrom(eventArgs.ApplicationMessage.Payload);
            var payload = protoPayload.ToPayload();

            var messageContext = new SparkplugMessageEventArgs(version, messageType, groupId!, edgeNodeId!, deviceId,
                payload, eventArgs);

            // Process messages based on the message type
            await (messageType switch
            {
                NDATA or DDATA => ProcessDataMessagesAsync(messageContext),
                NBIRTH or DBIRTH or NDEATH or DDEATH => ProcessBirthDeathMessagesAsync(messageContext),
                NCMD or DCMD => Task.CompletedTask, // Ignore command messages
                _ => throw new NotSupportedException(
                    $"Not supported Sparkplug message type {messageType} for Host Application.")
            });
        }
        catch (Exception)
        {
            try
            {
                // Raise the UnsupportedReceived event
                await _events.UnsupportedReceivedEvent.InvokeAsync(eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception occurred while handling unsupported message received event");
            }
        }
    }

    /// <summary>
    ///     Processes data messages (NDATA and DDATA) and invokes the appropriate event.
    ///     The Sparkplug specification states that the sequence numbers of NDATA and DDATA messages must be incremented
    ///     sequentially. If a message with an out-of-order sequence number is received, a Reorder Timeout timer
    ///     should be started. Missing messages must arrive before this timeout elapses. If missing messages
    ///     do not arrive within the timeout period, a Rebirth Request should be sent to the Edge Node/Device.
    ///     If missing messages arrive before the timeout, normal operation continues. This implementation will
    ///     be updated to fully comply with the Sparkplug specification as documented in MessageOrdering.md.
    /// </summary>
    /// <param name="messageContext">The message context containing all necessary information.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ProcessDataMessagesAsync(SparkplugMessageEventArgs messageContext)
    {
        // Initialize the pending messages based on the message ordering configuration
        var messages = _sparkplugOptions.EnableMessageOrdering
            ? _msgOrderingService.ProcessMessageOrder(messageContext)
            : [messageContext];

        // Process all messages in order
        await HandlePendingMessages(messages);
    }

    /// <summary>
    ///     Processes life cycle messages (NBIRTH, NDEATH, DBIRTH, DDEATH) and invokes the appropriate event.
    ///     The Sparkplug specification states that the sequence numbers of NBIRTH and DBIRTH messages must be 0, while
    ///     NDEATH and DDEATH messages do not carry a sequence number. In practice, to ensure better compatibility, we
    ///     uniformly clear the caches when receiving BIRTH and DEATH messages.
    /// </summary>
    /// <param name="messageContext">The message context containing all necessary information.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ProcessBirthDeathMessagesAsync(SparkplugMessageEventArgs messageContext)
    {
        if (_sparkplugOptions.EnableMessageOrdering)
            // Clear message order cache for the edge node or device
            _msgOrderingService.ClearMessageOrderCache(messageContext.GroupId, messageContext.EdgeNodeId,
                messageContext.DeviceId);

        try
        {
            // Raise the appropriate event
            await (messageContext.MessageType switch
            {
                NBIRTH => _events.EdgeNodeBirthReceivedEvent.InvokeAsync(messageContext),
                NDEATH => _events.EdgeNodeDeathReceivedEvent.InvokeAsync(messageContext),
                DBIRTH => _events.DeviceBirthReceivedEvent.InvokeAsync(messageContext),
                DDEATH => _events.DeviceDeathReceivedEvent.InvokeAsync(messageContext),
                _ => Task.CompletedTask // This case should never be reached due to the method's caller check
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Exception occurred while handling message received event. GroupId: {GroupId}, EdgeNodeId: {EdgeNodeId}, DeviceId: {DeviceId}",
                messageContext.GroupId, messageContext.EdgeNodeId, messageContext.DeviceId);
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

            try
            {
                // Raise the DisconnectedReceived event
                await _events.DisconnectedReceivedEvent.InvokeAsync(eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception occurred while handling client disconnected event");
            }
        }
    }

    #region Events

    public event Func<SparkplugMessageEventArgs, Task> EdgeNodeBirthReceivedAsync
    {
        add => _events.EdgeNodeBirthReceivedEvent.AddHandler(value);
        remove => _events.EdgeNodeBirthReceivedEvent.RemoveHandler(value);
    }

    public event Func<SparkplugMessageEventArgs, Task> EdgeNodeDeathReceivedAsync
    {
        add => _events.EdgeNodeDeathReceivedEvent.AddHandler(value);
        remove => _events.EdgeNodeDeathReceivedEvent.RemoveHandler(value);
    }

    public event Func<SparkplugMessageEventArgs, Task> EdgeNodeDataReceivedAsync
    {
        add => _events.EdgeNodeDataReceivedEvent.AddHandler(value);
        remove => _events.EdgeNodeDataReceivedEvent.RemoveHandler(value);
    }

    public event Func<SparkplugMessageEventArgs, Task> DeviceBirthReceivedAsync
    {
        add => _events.DeviceBirthReceivedEvent.AddHandler(value);
        remove => _events.DeviceBirthReceivedEvent.RemoveHandler(value);
    }

    public event Func<SparkplugMessageEventArgs, Task> DeviceDeathReceivedAsync
    {
        add => _events.DeviceDeathReceivedEvent.AddHandler(value);
        remove => _events.DeviceDeathReceivedEvent.RemoveHandler(value);
    }

    public event Func<SparkplugMessageEventArgs, Task> DeviceDataReceivedAsync
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

    #endregion
}