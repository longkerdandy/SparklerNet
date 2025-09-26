using MQTTnet;
using MQTTnet.Internal;

namespace SparklerNet.Core.Events;

public sealed class SparkplugMessageEvents
{
    public AsyncEvent<EdgeNodeMessageEventArgs> EdgeNodeBirthReceivedEvent { get; } = new();
    public AsyncEvent<EdgeNodeMessageEventArgs> EdgeNodeDeathReceivedEvent { get; } = new();
    public AsyncEvent<EdgeNodeMessageEventArgs> EdgeNodeDataReceivedEvent { get; } = new();
    public AsyncEvent<DeviceMessageEventArgs> DeviceBirthReceivedEvent { get; } = new();
    public AsyncEvent<DeviceMessageEventArgs> DeviceDeathReceivedEvent { get; } = new();
    public AsyncEvent<DeviceMessageEventArgs> DeviceDataReceivedEvent { get; } = new();
    public AsyncEvent<HostApplicationMessageEventArgs> StateReceivedEvent { get; } = new();

    public AsyncEvent<MqttApplicationMessageReceivedEventArgs> UnsupportedReceivedEvent { get; } = new();
}