using MQTTnet;

namespace SparklerNet.Core.Events;

/// <summary>
///     Event arguments for Sparkplug connected events.
/// </summary>
/// <param name="connectResult">The result of the MQTT client connect operation</param>
/// <param name="subscribeResult">The result of the MQTT client subscribe operation</param>
public sealed class ConnectedEventArgs(MqttClientConnectResult connectResult, MqttClientSubscribeResult subscribeResult)
    : EventArgs
{
    /// <summary>
    ///     The result of the MQTT client connect operation
    /// </summary>
    public MqttClientConnectResult ConnectResult { get; init; } = connectResult;

    /// <summary>
    ///     The result of the MQTT client subscribe operation
    /// </summary>
    public MqttClientSubscribeResult SubscribeResult { get; init; } = subscribeResult;
}