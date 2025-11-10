using MQTTnet;
using MQTTnet.Internal;

namespace SparklerNet.Core.Events;

/// <summary>
///     Async events for Sparkplug message received events.
/// </summary>
public sealed class SparkplugMessageEvents
{
    // NBIRTH Event
    public AsyncEvent<SparkplugMessageEventArgs> EdgeNodeBirthReceivedEvent { get; } = new();

    // NDEATH Event
    public AsyncEvent<SparkplugMessageEventArgs> EdgeNodeDeathReceivedEvent { get; } = new();

    // NDATA Event
    public AsyncEvent<SparkplugMessageEventArgs> EdgeNodeDataReceivedEvent { get; } = new();

    // DBirth Event
    public AsyncEvent<SparkplugMessageEventArgs> DeviceBirthReceivedEvent { get; } = new();

    // DDeath Event
    public AsyncEvent<SparkplugMessageEventArgs> DeviceDeathReceivedEvent { get; } = new();

    // DDATA Event
    public AsyncEvent<SparkplugMessageEventArgs> DeviceDataReceivedEvent { get; } = new();

    // STATE Event
    public AsyncEvent<HostApplicationMessageEventArgs> StateReceivedEvent { get; } = new();

    // Sparkplug client connected event
    public AsyncEvent<ConnectedEventArgs> ConnectedReceivedEvent { get; } = new();

    // Sparkplug client disconnected event
    public AsyncEvent<MqttClientDisconnectedEventArgs> DisconnectedReceivedEvent { get; } = new();

    // Unsupported Sparkplug message received event
    public AsyncEvent<MqttApplicationMessageReceivedEventArgs> UnsupportedReceivedEvent { get; } = new();
}