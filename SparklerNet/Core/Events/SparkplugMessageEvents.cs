using MQTTnet;
using MQTTnet.Internal;

namespace SparklerNet.Core.Events;

/// <summary>
///     Async events for Sparkplug message received events.
/// </summary>
public sealed class SparkplugMessageEvents
{
    // NBIRTH Event
    public AsyncEvent<EdgeNodeMessageEventArgs> EdgeNodeBirthReceivedEvent { get; } = new();

    // NDEATH Event
    public AsyncEvent<EdgeNodeMessageEventArgs> EdgeNodeDeathReceivedEvent { get; } = new();

    // NDATA Event
    public AsyncEvent<EdgeNodeMessageEventArgs> EdgeNodeDataReceivedEvent { get; } = new();

    // DBirth Event
    public AsyncEvent<DeviceMessageEventArgs> DeviceBirthReceivedEvent { get; } = new();

    // DDeath Event
    public AsyncEvent<DeviceMessageEventArgs> DeviceDeathReceivedEvent { get; } = new();

    // DDATA Event
    public AsyncEvent<DeviceMessageEventArgs> DeviceDataReceivedEvent { get; } = new();

    // STATE Event
    public AsyncEvent<HostApplicationMessageEventArgs> StateReceivedEvent { get; } = new();

    // Sparkplug client connected event
    public AsyncEvent<ConnectedEventArgs> ConnectedReceivedEvent { get; } = new();

    // Unsupported Sparkplug message received event
    public AsyncEvent<MqttApplicationMessageReceivedEventArgs> UnsupportedReceivedEvent { get; } = new();
}