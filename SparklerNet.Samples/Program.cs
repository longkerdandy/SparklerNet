using MQTTnet;
using MQTTnet.Formatter;
using Serilog;
using Serilog.Extensions.Logging;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Options;

namespace SparklerNet.Samples;

internal static class Program
{
    public static async Task Main()
    {
        // Configure Serilog logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        // Display welcome message
        DisplayWelcomeMessage();

        // Create client options
        var mqttOptions = CreateMqttOptions();
        var sparkplugOptions = CreateSparkplugOptions();

        // Create logger factory and host application
        using var loggerFactory = new SerilogLoggerFactory(Log.Logger);
        var hostApp = new SimpleHostApplication(mqttOptions, sparkplugOptions, loggerFactory);
        hostApp.SubscribeToEvents();

        // Handle console input and process commands
        await hostApp.HandleConsoleInputAsync();

        // Flush logs before exiting
        await Log.CloseAndFlushAsync();
    }
    
    /// <summary>
    /// Displays the application welcome message with ASCII art and command instructions.
    /// </summary>
    private static void DisplayWelcomeMessage()
    {
        Console.WriteLine("====================================================");
        Console.WriteLine("  SSSSS PPPP  AAAAA  RRRRR  K   K L     EEEEE RRRRR ");
        Console.WriteLine(" S      P   P A   A  R   R  K  K  L     E     R   R ");
        Console.WriteLine("  SSSS  PPPP  AAAAA  RRRRR  KKK   L     EEEE  RRRRR ");
        Console.WriteLine("     S  P     A   A  R  R   K  K  L     E     R  R  ");
        Console.WriteLine(" SSSSS  P     A   A  R   R  K   K LLLLL EEEEE R   R ");
        Console.WriteLine("                                                    ");
        Console.WriteLine("                SparklerNet                         ");
        Console.WriteLine("====================================================");
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
    }

    /// <summary>
    /// Creates and configures MQTT client options.
    /// </summary>
    /// <returns>Configured MqttClientOptions instance.</returns>
    private static MqttClientOptions CreateMqttOptions()
    {
        return new MqttClientOptionsBuilder()
            .WithTcpServer("BROKER.HIVEMQ.COM", 1883)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .Build();
    }

    /// <summary>
    /// Creates and configures Sparkplug client options.
    /// </summary>
    /// <returns>Configured SparkplugClientOptions instance.</returns>
    private static SparkplugClientOptions CreateSparkplugOptions()
    {
        return new SparkplugClientOptions
        {
            Version = SparkplugVersion.V300,
            HostApplicationId = "SparklerNetSimpleHostApp",
            Subscriptions = { new MqttTopicFilterBuilder().WithTopic("spBv1.0/MIMIC/#").WithAtLeastOnceQoS().Build() },
            EnableMessageOrdering = true
        };
    }
}