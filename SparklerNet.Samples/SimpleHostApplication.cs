using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Formatter;
using Serilog;
using Serilog.Extensions.Logging;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Events;
using SparklerNet.Core.Model;
using SparklerNet.Core.Options;
using SparklerNet.HostApplication;
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
    ///     Constructor for SimpleHostApplication
    /// </summary>
    /// <param name="mqttOptions">MQTT client options</param>
    /// <param name="sparkplugOptions">Sparkplug client options</param>
    /// <param name="logger">Logger instance for SparkplugHostApplication</param>
    /// <param name="simpleHostLogger">Logger instance for SimpleHostApplication</param>
    private SimpleHostApplication(MqttClientOptions mqttOptions, SparkplugClientOptions sparkplugOptions,
        ILogger<SparkplugHostApplication> logger, ILogger<SimpleHostApplication> simpleHostLogger)
    {
        _hostApplication = new SparkplugHostApplication(mqttOptions, sparkplugOptions, logger);
        _logger = simpleHostLogger;
        _isRunning = false;
    }

    /// <summary>
    ///     Initialize and run the SimpleHostApplication
    /// </summary>
    [SuppressMessage("ReSharper", "RedundantVerbatimStringPrefix")]
    public static async Task Main()
    {
        // Configure Serilog logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            // Display ASCII art and welcome message
            Console.WriteLine(@"====================================================");
            Console.WriteLine(@"  _____              _       _                      ");
            Console.WriteLine(@" / ____|            | |     | |                     ");
            Console.WriteLine(@"| (___   ___   ___  | |_ ___| |_ _   _ _ __         ");
            Console.WriteLine(@" \_\__ \ / _ \ / _ \ | __/ _ \ __| | | | '_ \       ");
            Console.WriteLine(@" ____) | (_) |  __/ | ||  __/ |_| |_| | |_) |       ");
            Console.WriteLine(@"|_____/ \___/ \___|  \__\___|\__|\__,_| .__/        ");
            Console.WriteLine(@"                                      | |           ");
            Console.WriteLine(@"                                      |_|           ");
            Console.WriteLine(@"             HOST APPLICATION                       ");
            Console.WriteLine(@"====================================================");
            Console.WriteLine("A simple implementation of the Sparkplug Host");
            Console.WriteLine("Application protocol for industrial IoT communication");
            Console.WriteLine();
            Console.WriteLine("AVAILABLE COMMANDS:");
            Console.WriteLine("  start   - Initialize and connect to the MQTT broker");
            Console.WriteLine("  stop    - Disconnect from the MQTT broker and shutdown");
            Console.WriteLine("  ncmd    - Send a Rebirth command message to an Edge Node");
            Console.WriteLine("  dcmd    - Send a Rebirth command message to a Device");
            Console.WriteLine("  exit    - Exit the application");
            Console.WriteLine();
            Console.WriteLine("Enter command to begin:");

            // Create MQTT client options
            var mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", 1883)
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .Build();

            // Create Sparkplug client options
            var sparkplugOptions = new SparkplugClientOptions
            {
                HostApplicationId = "SampleHostApp",
                Version = SparkplugVersion.V300
            };

            // Create a Simple Host Application instance
            var loggerFactory = new SerilogLoggerFactory(Log.Logger);
            var simpleHostApp = new SimpleHostApplication(
                mqttOptions,
                sparkplugOptions,
                loggerFactory.CreateLogger<SparkplugHostApplication>(),
                loggerFactory.CreateLogger<SimpleHostApplication>());

            // Subscribe to events
            simpleHostApp.SubscribeToEvents();

            // Handle console input
            await simpleHostApp.HandleConsoleInputAsync();
        }
        finally
        {
            // Clean up logging
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    ///     Subscribe to relevant events in HostApplication
    /// </summary>
    private void SubscribeToEvents()
    {
        _hostApplication.ConnectedReceivedAsync += HandleConnectedEvent;
        _hostApplication.DisconnectedReceivedAsync += HandleDisconnectedEvent;
        _hostApplication.EdgeNodeBirthReceivedAsync += args => HandleEdgeNodeMessage(NBIRTH, args);
        _hostApplication.EdgeNodeDataReceivedAsync += args => HandleEdgeNodeMessage(NDATA, args);
        _hostApplication.EdgeNodeDeathReceivedAsync += args => HandleEdgeNodeMessage(NDEATH, args);
        _hostApplication.DeviceBirthReceivedAsync += args => HandleDeviceMessage(DBIRTH, args);
        _hostApplication.DeviceDataReceivedAsync += args => HandleDeviceMessage(DDATA, args);
        _hostApplication.DeviceDeathReceivedAsync += args => HandleDeviceMessage(DDEATH, args);
        _hostApplication.StateReceivedAsync += HandleStateMessage;
        _hostApplication.UnsupportedReceivedAsync += HandleUnsupportedMessage;
    }

    private Task HandleConnectedEvent(ConnectedEventArgs args)
    {
        _logger.LogInformation("Connected to MQTT broker with result code: {ResultCode}",
            args.ConnectResult.ResultCode);
        _logger.LogInformation("Successfully subscribed to {Count} topics", args.SubscribeResult.Items.Count);
        return Task.CompletedTask;
    }

    private Task HandleDisconnectedEvent(MqttClientDisconnectedEventArgs args)
    {
        _logger.LogWarning("Disconnected from MQTT broker: {Reason}", args.Reason);
        return Task.CompletedTask;
    }

    private Task HandleStateMessage(HostApplicationMessageEventArgs args)
    {
        _logger.LogInformation(
            "Received {MessageType} message from Host={HostId}: Online={Online}, Timestamp={Timestamp}",
            STATE, args.HostId, args.Payload.Online, args.Payload.Timestamp);
        return Task.CompletedTask;
    }

    private Task HandleUnsupportedMessage(MqttApplicationMessageReceivedEventArgs args)
    {
        _logger.LogWarning("Received Unsupported message from Topic={Topic}", args.ApplicationMessage.Topic);
        return Task.CompletedTask;
    }

    private Task HandleEdgeNodeMessage(SparkplugMessageType messageType, EdgeNodeMessageEventArgs args)
    {
        _logger.LogInformation("Received {MessageType} message from Group={Group}, Node={Node}",
            messageType, args.GroupId, args.EdgeNodeId);
        LogPayloadData(args.Payload);
        return Task.CompletedTask;
    }

    private Task HandleDeviceMessage(SparkplugMessageType messageType, DeviceMessageEventArgs args)
    {
        _logger.LogInformation("Received {MessageType} message from Group={Group}, Node={Node}, Device={Device}",
            messageType, args.GroupId, args.EdgeNodeId, args.DeviceId);
        LogPayloadData(args.Payload);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handle console input commands
    /// </summary>
    private async Task HandleConsoleInputAsync()
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
        if (_isRunning)
        {
            _logger.LogInformation("Stopping Sparkplug Host Application before exit...");
            await _hostApplication.StopAsync();
        }
    }

    /// <summary>
    ///     Start the host application
    /// </summary>
    private async Task StartHostApplicationAsync()
    {
        if (!_isRunning)
            try
            {
                var (connectResult, _) = await _hostApplication.StartAsync();
                _isRunning = connectResult.ResultCode == MqttClientConnectResultCode.Success;

                if (!_isRunning) _logger.LogError("Failed to start: {ResultCode}", connectResult.ResultCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while starting host application");
            }
        else
            _logger.LogInformation("Host application is already running.");
    }

    /// <summary>
    ///     Stop the host application
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
            _logger.LogInformation("Host application is not running.");
    }

    /// <summary>
    ///     Send command to the edge node or device
    /// </summary>
    /// <param name="isNodeCommand">True to send node command (NCMD), false to send device command (DCMD)</param>
    private async Task SendCommandAsync(bool isNodeCommand)
    {
        try
        {
            // Show default value in prompt for better user experience
            const string defaultGroupId = "Sparkplug Group 1";
            Console.Write($"Enter Group ID [{defaultGroupId}]: ");
            var groupIdInput = Console.ReadLine();
            var groupId = string.IsNullOrWhiteSpace(groupIdInput) ? defaultGroupId : groupIdInput;

            const string defaultEdgeNodeId = "Sparkplug Node 1";
            Console.Write($"Enter Edge Node ID [{defaultEdgeNodeId}]: ");
            var edgeNodeIdInput = Console.ReadLine();
            var edgeNodeId = string.IsNullOrWhiteSpace(edgeNodeIdInput) ? defaultEdgeNodeId : edgeNodeIdInput;

            // Create the payload for rebirth command
            var payload = CreateRebirthPayload(isNodeCommand);

            if (isNodeCommand)
            {
                _logger.LogInformation("Sending NCMD to {Group}/{Node}", groupId, edgeNodeId);
                await _hostApplication.PublishEdgeNodeCommandMessageAsync(groupId, edgeNodeId, payload);
            }
            else
            {
                // Show default value in prompt for better user experience
                const string defaultDeviceId = "Sparkplug Device 1";
                Console.Write($"Enter Device ID [{defaultDeviceId}]: ");
                var deviceIdInput = Console.ReadLine();
                var deviceId = string.IsNullOrWhiteSpace(deviceIdInput) ? defaultDeviceId : deviceIdInput;

                _logger.LogInformation("Sending DCMD to {Group}/{Node}/{Device}", groupId, edgeNodeId, deviceId);
                await _hostApplication.PublishDeviceCommandMessageAsync(groupId, edgeNodeId, deviceId, payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {CommandType} command", isNodeCommand ? "edge node" : "device");
        }
    }


    /// <summary>
    ///     Create a rebirth payload
    /// </summary>
    /// <param name="isNodeCommand">True for NCMD (Node Command), False for DCMD (Device Command)</param>
    /// <returns>Payload object configured for rebirth command</returns>
    private static Payload CreateRebirthPayload(bool isNodeCommand = true)
    {
        return new Payload
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Metrics =
            {
                new Metric
                {
                    Name = isNodeCommand ? "Node Control/Rebirth" : "Device Control/Rebirth",
                    DateType = DataType.Boolean,
                    Value = true
                }
            }
        };
    }

    /// <summary>
    ///     Log payload data (merged EdgeNode and Device data logging)
    /// </summary>
    /// <param name="payload">Payload to log</param>
    private void LogPayloadData(Payload payload)
    {
        _logger.LogInformation("  Timestamp: {Timestamp}", payload.Timestamp);
        _logger.LogInformation("  Sequence: {Sequence}", payload.Seq);
        if (!(payload.Metrics.Count > 0)) return;
        _logger.LogInformation("  Metrics ({Count}):", payload.Metrics.Count);
        foreach (var metric in payload.Metrics)
            _logger.LogInformation("    - {Name} {Type}: {Value}", metric.Name, metric.DateType, metric.Value);
    }
}