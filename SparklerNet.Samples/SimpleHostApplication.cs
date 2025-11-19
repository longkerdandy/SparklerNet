using Microsoft.Extensions.Logging;
using MQTTnet;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Events;
using SparklerNet.Core.Model;
using SparklerNet.Core.Options;
using SparklerNet.HostApplication;
using SparklerNet.HostApplication.Extensions;
using static SparklerNet.Core.Constants.SparkplugMessageType;

namespace SparklerNet.Samples;

/// <summary>
///     A simple Sparkplug host application implementation
/// </summary>
public class SimpleHostApplication
{
    private readonly SparkplugHostApplication _hostApplication;
    private readonly ILogger<SimpleHostApplication> _logger;
    private bool _isRunning;

    /// <summary>
    ///     Initializes a new instance of the SimpleHostApplication class.
    /// </summary>
    /// <param name="mqttOptions">MQTT client options</param>
    /// <param name="sparkplugOptions">Sparkplug client options</param>
    /// <param name="loggerFactory">Logger factory to create required loggers</param>
    public SimpleHostApplication(MqttClientOptions mqttOptions, SparkplugClientOptions sparkplugOptions,
        ILoggerFactory loggerFactory)
    {
        _hostApplication = new SparkplugHostApplication(mqttOptions, sparkplugOptions, loggerFactory);
        _logger = loggerFactory.CreateLogger<SimpleHostApplication>();
        _isRunning = false;
    }

    /// <summary>
    ///     Subscribes to Sparkplug message events for processing.
    /// </summary>
    public void SubscribeToEvents()
    {
        _hostApplication.EdgeNodeBirthReceivedAsync += args => HandleEdgeNodeMessage(NBIRTH, args);
        _hostApplication.EdgeNodeDataReceivedAsync += args => HandleEdgeNodeMessage(NDATA, args);
        _hostApplication.EdgeNodeDeathReceivedAsync += args => HandleEdgeNodeMessage(NDEATH, args);
        _hostApplication.DeviceBirthReceivedAsync += args => HandleDeviceMessage(DBIRTH, args);
        _hostApplication.DeviceDataReceivedAsync += args => HandleDeviceMessage(DDATA, args);
        _hostApplication.DeviceDeathReceivedAsync += args => HandleDeviceMessage(DDEATH, args);
        _hostApplication.UnsupportedReceivedAsync += HandleUnsupportedMessage;
    }

    /// <summary>
    ///     Handles unsupported MQTT messages.
    /// </summary>
    private Task HandleUnsupportedMessage(MqttApplicationMessageReceivedEventArgs args)
    {
        _logger.LogWarning("Received Unsupported message from Topic={Topic}", args.ApplicationMessage.Topic);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles edge node message events.
    /// </summary>
    private Task HandleEdgeNodeMessage(SparkplugMessageType messageType, SparkplugMessageEventArgs args)
    {
        _logger.LogInformation("Received {MessageType} message from Group={Group}, Node={Node}, Seq={Seq}",
            messageType, args.GroupId, args.EdgeNodeId, args.Payload.Seq);
        LogPayloadData(args.Payload);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles device message events.
    /// </summary>
    private Task HandleDeviceMessage(SparkplugMessageType messageType, SparkplugMessageEventArgs args)
    {
        _logger.LogInformation(
            "Received {MessageType} message from Group={Group}, Node={Node}, Device={Device}, Seq={Seq}", messageType,
            args.GroupId, args.EdgeNodeId, args.DeviceId, args.Payload.Seq);
        LogPayloadData(args.Payload);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Processes console input commands to control the host application.
    /// </summary>
    public async Task HandleConsoleInputAsync()
    {
        string? input;
        while ((input = Console.ReadLine()) != "exit")
        {
            // Check for null input which can occur with EOF
            if (input == null) break;

            try
            {
                await (input.ToLower() switch
                {
                    "start" => StartHostApplicationAsync(),
                    "stop" => StopHostApplicationAsync(),
                    "ncmd" => _isRunning
                        ? SendCommandAsync(true)
                        : Task.Run(() =>
                            Console.WriteLine("Host application is not running. Please start it first.")),
                    "dcmd" => _isRunning
                        ? SendCommandAsync(false)
                        : Task.Run(() =>
                            Console.WriteLine("Host application is not running. Please start it first.")),
                    _ => Task.Run(() =>
                        Console.WriteLine("Unknown command. Available commands: start, stop, ncmd, dcmd, exit"))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command: {Command}", input);
            }
        }

        // Ensure the application is stopped before exiting
        if (_isRunning) await _hostApplication.StopAsync();
    }

    /// <summary>
    ///     Starts the Sparkplug host application and connects to the MQTT broker.
    /// </summary>
    private async Task StartHostApplicationAsync()
    {
        if (!_isRunning)
            try
            {
                var (connectResult, _) = await _hostApplication.StartAsync();
                _isRunning = connectResult.ResultCode == MqttClientConnectResultCode.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while starting host application");
            }
        else
            _logger.LogInformation("Sparkplug Host application is already running.");
    }

    /// <summary>
    ///     Stops the Sparkplug host application and disconnects from the MQTT broker.
    /// </summary>
    private async Task StopHostApplicationAsync()
    {
        if (_isRunning)
            try
            {
                await _hostApplication.StopAsync();
                _isRunning = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while stopping host application");
            }
        else
            _logger.LogInformation("Sparkplug Host application is not running.");
    }

    /// <summary>
    ///     Sends a rebirth command to either an edge node or device.
    /// </summary>
    /// <param name="isNodeCommand">True to send node command (NCMD), false to send device command (DCMD)</param>
    private async Task SendCommandAsync(bool isNodeCommand)
    {
        try
        {
            const string defaultGroupId = "Sparkplug_Group_1";
            Console.Write($"Enter Group ID [{defaultGroupId}]: ");
            var groupIdInput = Console.ReadLine();
            var groupId = string.IsNullOrWhiteSpace(groupIdInput) ? defaultGroupId : groupIdInput;

            const string defaultEdgeNodeId = "Sparkplug_Node_1";
            Console.Write($"Enter Edge Node ID [{defaultEdgeNodeId}]: ");
            var edgeNodeIdInput = Console.ReadLine();
            var edgeNodeId = string.IsNullOrWhiteSpace(edgeNodeIdInput) ? defaultEdgeNodeId : edgeNodeIdInput;

            string? deviceId = null;
            if (!isNodeCommand)
            {
                const string defaultDeviceId = "Sparkplug_Device_1";
                Console.Write($"Enter Device ID [{defaultDeviceId}]: ");
                var deviceIdInput = Console.ReadLine();
                deviceId = string.IsNullOrWhiteSpace(deviceIdInput) ? defaultDeviceId : deviceIdInput;
            }

            const string defaultCommandType = "Rebirth";
            string commandType;
            bool isValid;
            do
            {
                Console.Write($"Enter Command Type (Rebirth/ScanRate) [{defaultCommandType}]: ");
                var commandTypeInput = Console.ReadLine();
                commandType = string.IsNullOrWhiteSpace(commandTypeInput)
                    ? defaultCommandType
                    : commandTypeInput.Trim();

                // Validate command type with single check
                isValid = commandType is "Rebirth" or "ScanRate";
                if (!isValid) Console.WriteLine("Invalid command type. Please enter 'Rebirth' or 'ScanRate'.");
            } while (!isValid);

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (isNodeCommand && commandType == "Rebirth")
            {
                _logger.LogInformation("Sending NCMD Rebirth to {Group}/{Node}", groupId, edgeNodeId);
                await _hostApplication.PublishEdgeNodeRebirthCommandAsync(groupId, edgeNodeId);
            }
            else if (isNodeCommand && commandType == "ScanRate")
            {
                var randomScanRate = new Random().Next(1000, 10001);
                _logger.LogInformation("Sending NCMD ScanRate ({ScanRate}ms) to {Group}/{Node}", randomScanRate,
                    groupId, edgeNodeId);
                await _hostApplication.PublishEdgeNodeScanRateCommandAsync(groupId, edgeNodeId, randomScanRate);
            }
            else if (!isNodeCommand && commandType == "Rebirth")
            {
                _logger.LogInformation("Sending DCMD Rebirth to {Group}/{Node}/{Device}", groupId, edgeNodeId,
                    deviceId!);
                await _hostApplication.PublishDeviceRebirthCommandAsync(groupId, edgeNodeId, deviceId!);
            }
            else if (!isNodeCommand && commandType == "ScanRate")
            {
                var randomScanRate = new Random().Next(1000, 10001);
                _logger.LogInformation("Sending DCMD ScanRate ({ScanRate}ms) to {Group}/{Node}/{Device}",
                    randomScanRate, groupId, edgeNodeId, deviceId!);
                await _hostApplication.PublishDeviceScanRateCommandAsync(groupId, edgeNodeId, deviceId!,
                    randomScanRate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {CommandType} command", isNodeCommand ? "edge node" : "device");
        }
    }

    /// <summary>
    ///     Logs payload data including timestamp, sequence, and metrics.
    /// </summary>
    /// <param name="payload">Payload to log</param>
    private void LogPayloadData(Payload payload)
    {
        _logger.LogDebug("  Timestamp: {Timestamp}", payload.Timestamp);
        _logger.LogDebug("  Sequence: {Sequence}", payload.Seq);
        if (!(payload.Metrics.Count > 0)) return;
        _logger.LogDebug("  Metrics ({Count}):", payload.Metrics.Count);
        foreach (var metric in payload.Metrics)
            _logger.LogDebug("    - {Name} {Type}: {Value}", metric.Name, metric.DataType, metric.Value);
    }
}