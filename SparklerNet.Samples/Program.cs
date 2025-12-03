using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using SparklerNet.HostApplication;
using SparklerNet.HostApplication.Caches;
using SparklerNet.Samples.Profiles;

namespace SparklerNet.Samples;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        // Display welcome message
        DisplayWelcomeMessage();

        // Parse command line arguments and determine which profile to use
        var profileName = ParseProfileArgument(args);
        var profile = CreateProfile(profileName);
        Log.Information("Using profile: {ProfileName}", profileName);

        // Get client options from the selected profile
        var mqttOptions = profile.GetMqttClientOptions();
        var sparkplugOptions = profile.GetSparkplugClientOptions();

        // Create the logger factory
        using var loggerFactory = new SerilogLoggerFactory(Log.Logger);

        // Create the dependency injection container
        var services = new ServiceCollection();

        // Register the singleton services
        services.AddSingleton(mqttOptions);
        services.AddSingleton(sparkplugOptions);
        services.AddSingleton(loggerFactory);
        services.AddSingleton<ILoggerFactory>(loggerFactory);

        // Register the SparklerNet services
        services.AddSingleton<IMessageOrderingCache, MessageOrderingCache>();
        services.AddSingleton<IStatusTrackingService, StatusTrackingService>();
        services.AddSingleton<SparkplugHostApplication>();

        // Register the SimpleHostApplication
        services.AddSingleton<SimpleHostApplication>();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Resolve SimpleHostApplication from the container
        var hostApp = serviceProvider.GetRequiredService<SimpleHostApplication>();
        hostApp.SubscribeToEvents();

        // Handle console input and process commands
        await hostApp.HandleConsoleInputAsync();

        // Flush logs before exiting
        await Log.CloseAndFlushAsync();
    }

    /// <summary>
    ///     Displays the application welcome message with ASCII art and command instructions.
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
    ///     Parses the command line arguments to determine which profile to use.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The profile name.</returns>
    private static string ParseProfileArgument(string[] args)
    {
        const string defaultProfile = "mimic";
        const string profileArg = "--profile";

        // Check if --profile argument is provided
        for (var i = 0; i < args.Length; i++)
            if (args[i].Equals(profileArg, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1].ToLower();

        // Default to the mimic profile if no profile is specified
        return defaultProfile;
    }

    /// <summary>
    ///     Creates the appropriate profile based on the profile name.
    /// </summary>
    /// <param name="profileName">The name of the profile to create.</param>
    /// <returns>The created profile instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid profile name is provided.</exception>
    private static IProfile CreateProfile(string profileName)
    {
        return profileName switch
        {
            "tck" => new TckApplicationProfile(),
            "mimic" => new MimicApplicationProfile(),
            _ => throw new ArgumentOutOfRangeException(nameof(profileName), profileName, null)
        };
    }
}