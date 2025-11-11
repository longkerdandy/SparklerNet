using System.Net;
using MQTTnet;

namespace SparklerNet.Core.Extensions;

/// <summary>
///     Extension methods for MqttClientOptions
/// </summary>
public static class MqttClientOptionsExtensions
{
    /// <summary>
    ///     Retrieves the broker URL from the provided MQTT client options.
    /// </summary>
    /// <param name="options">The MQTT client options.</param>
    /// <returns>The broker URL in the format "Host:Port".</returns>
    public static string GetBrokerUrl(this MqttClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            var tcpOptions = (MqttClientTcpOptions)options.ChannelOptions;
            var endPoint = (DnsEndPoint)tcpOptions.RemoteEndpoint;
            return $"{endPoint.Host}:{endPoint.Port}";
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}