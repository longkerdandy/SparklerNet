# SparklerNet

[![License](https://img.shields.io/github/license/longkerdandy/SparklerNet)](https://github.com/longkerdandy/SparklerNet/blob/main/LICENSE)
[![Language](https://img.shields.io/github/languages/top/longkerdandy/SparklerNet)](https://github.com/longkerdandy/SparklerNet)
[![Build & Test](https://github.com/longkerdandy/SparklerNet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/longkerdandy/SparklerNet/actions/workflows/dotnet.yml)
[![CodeQL Advanced](https://github.com/longkerdandy/SparklerNet/actions/workflows/codeql.yml/badge.svg)](https://github.com/longkerdandy/SparklerNet/actions/workflows/codeql.yml)
[![NuGet Version](https://img.shields.io/nuget/v/SparklerNet)](https://www.nuget.org/packages/SparklerNet/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SparklerNet)](https://www.nuget.org/packages/SparklerNet/)

## Project Introduction

SparklerNet is an open-source Sparkplug Client Library for .NET 9.0+ that implements the [Eclipse Sparkplug B Specification](https://www.eclipse.org/sparkplug/). Built on top of [MQTTnet](https://github.com/dotnet/MQTTnet), a robust open-source MQTT client library, SparklerNet currently provides a complete implementation of the Host Application role, enabling .NET applications to seamlessly participate in Sparkplug-enabled IoT and Industrial IoT (IIoT) ecosystems.

The library aims to fully implement the complete Sparkplug protocol, with planned future support for Edge Node and Device roles. Sparkplug B itself is an open-source software specification designed to enhance interoperability in intelligent manufacturing and Industrial IoT, providing context for Operational Technology (OT) data to enable bidirectional seamless integration with Information Technology (IT).

## Core Features

- **Complete Sparkplug B Host Application Implementation**: Enables receiving data from multiple Sparkplug B Edge Nodes and sending commands to control outputs on Edge Nodes and Devices
- **Comprehensive Message Type Support**: Full implementation of all Sparkplug B message types (NBIRTH, NDEATH, NDATA, DBIRTH, DDEATH, DDATA, NCMD, DCMD, and STATE)
- **Event-Driven Architecture**: Provides asynchronous event handlers for processing all types of Sparkplug messages
- **Protobuf Message Serialization**: Leverages Google.Protobuf for efficient binary data processing according to the Sparkplug specification
- **MQTT 3.1.1 and 5.0 Support**: Compatible with both MQTT protocol versions with proper session handling
- **Flexible Logging**: Built on Microsoft.Extensions.Logging for integration with various logging frameworks

## System Requirements

- .NET 9.0 SDK or later
- MQTT protocol-compatible message broker (e.g., Mosquitto, HiveMQ)

## Roadmap

### Core Functionality

- ✅ Connection to MQTT brokers v3.1.1 or v5.0
- ✅ Connection using TCP, TCP+TLS and WebSockets
- ✅ Support for Sparkplug B data types (Int8/16/32/64, UInt8/16/32/64, Float, Double, Boolean, String, DateTime, Text, UUID, DataSet, Bytes, File, Template, PropertySet, PropertySetList)
- ✅ Support for Sparkplug B array data types (Int8Array, Int16Array, Int32Array, Int64Array, UInt8Array, UInt16Array, UInt32Array, UInt64Array, FloatArray, DoubleArray, BooleanArray, StringArray, DateTimeArray)

### Host Application

- ✅ Sparkplug-compliant Will message
- ✅ STATE message publishing (Birth/Death certificates)
- ✅ Default wildcard topic support (spBv1.0/#)
- ✅ Specific group and edge node subscription support
- ✅ Sparkplug Host Application Message Ordering
- ✅ Cache Edge Node and Device online status

### Message Processing

- ✅ Edge Node message handling (NBIRTH, NDEATH, NDATA)
- ✅ Device message handling (DBIRTH, DDEATH, DDATA)
- ✅ STATE message handling
- ✅ Error or unsupported message handling

### Command Capabilities

- ✅ Edge Node command publishing (NCMD)
- ✅ Device command publishing (DCMD)

### Event Notification System

- ✅ Comprehensive event model for all message types
- ✅ Connected/Disconnected events
- ✅ Error and unsupported message events

### Message Validation

- ✅ Host Application ID validation
- ✅ Group ID, Edge Node ID and Device ID validation

### Additional Features

- ⬜ Reconnection logic with exponential backoff
- ✅ Configuration validation

## Eclipse™ Sparkplug™ TCK Compatibility

The following are the compatibility test results against the Eclipse Sparkplug Test Compatibility Kit (TCK) available at https://github.com/eclipse-sparkplug/sparkplug/tree/master/tck:

### Host Application Tests

| Test | Status |
|------|--------|
| Session Establishment Test | ✅ Passed |
| Session Termination Test | ✅ Passed |
| Send Command Test | ✅ Passed |
| Edge Session Termination Test | ✅ Passed |
| Message Ordering Test | ✅ Passed |
| Multiple MQTT Server (Broker) Test | ❌ Not supported yet |

## Installation

Install SparklerNet via NuGet Package Manager:

```bash
dotnet add package SparklerNet
```

Or reference it directly in your project:

```xml
<ItemGroup>
  <PackageReference Include="SparklerNet" Version="0.9.*" />
</ItemGroup>
```

## Quick Start

Here's a simple example of a Sparkplug host application:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using SparklerNet.Core.Constants;
using SparklerNet.Core.Events;
using SparklerNet.HostApplication;
using SparklerNet.HostApplication.Caches;
using SparklerNet.HostApplication.Extensions;

// Create MQTT client options
var mqttOptions = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .WithProtocolVersion(MqttProtocolVersion.V500)
    .Build();

// Create Sparkplug client options
var sparkplugOptions = new SparkplugClientOptions
{
    Version = SparkplugVersion.V300,
    HostApplicationId = "MyHostApplication"
};

// Create the logger factory
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Create the dependency injection container
var services = new ServiceCollection();

// Register the singleton services
services.AddSingleton(mqttOptions);
services.AddSingleton(sparkplugOptions);
services.AddSingleton(loggerFactory);
services.AddSingleton<ILoggerFactory>(loggerFactory);

// Register cache services
services.AddMemoryCache();
services.AddHybridCache();

// Register the SparklerNet services
services.AddSingleton<IMessageOrderingService, MessageOrderingService>();
services.AddSingleton<IStatusTrackingService, StatusTrackingService>();
services.AddSingleton<SparkplugHostApplication>();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Resolve SparkplugHostApplication from the container
var hostApplication = serviceProvider.GetRequiredService<SparkplugHostApplication>();

// Subscribe to DBIRTH event
hostApplication.DeviceBirthReceivedAsync += args => {
    Console.WriteLine($"Received DBIRTH message from Group={args.GroupId}, Node={args.EdgeNodeId}, Device={args.DeviceId}");
    // Device birth message can be processed here
    return Task.CompletedTask;
};

// Start the application
await hostApplication.StartAsync();

// Example of sending a Device Rebirth command using the extension method
await hostApplication.PublishDeviceRebirthCommandAsync("myGroup", "myEdgeNode", "myDevice");

// Stop the application
await hostApplication.StopAsync();
```

## Sample Application

The project includes a sample named `SimpleHostApplication` demonstrating a complete Sparkplug Host Application implementation with these core features:

- **Interactive CLI**: User-friendly console interface for application lifecycle management and command sending
- **Full Event Processing**: Handles all Sparkplug message types with detailed data display
- **Command Capabilities**: Sends Rebirth and ScanRate commands to Edge Nodes (NCMD) and Devices (DCMD)
- **Configuration Profiles**: Supports multiple profiles (mimic, tck) via `--profile` command line argument
- **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection for service management
- **Structured Logging**: Implements Serilog for detailed message and processing logging

Refer to the `SparklerNet.Samples` project for the complete implementation.

## Project Structure

```
├── .gitattributes          # Git attributes file
├── .github/                # GitHub configuration files
│   └── workflows/          # GitHub Actions workflows
├── .gitignore              # Git ignore file
├── GIT_FLOW.md             # Git Flow workflow documentation
├── LICENSE                 # License file
├── README.md               # Project documentation
├── SparklerNet/            # Core library
│   ├── Core/               # Core functionality implementation
│   │   ├── Constants/      # Constant definitions
│   │   ├── Events/         # Event definitions
│   │   ├── Extensions/     # Extension methods
│   │   ├── Model/          # Data models
│   │   ├── Options/        # Configuration options
│   │   ├── Protobuf/       # Protobuf message definitions
│   │   └── Topics/         # Topic handling
│   ├── HostApplication/    # Host application implementation
│   │   ├── Caches/         # Caching mechanisms
│   │   ├── Extensions/     # Host application extension methods
│   │   └── SparkplugHostApplication.cs # Main host application class
│   ├── Properties/         # Assembly properties
│   │   └── AssemblyInfo.cs # Assembly information
│   └── SparklerNet.csproj  # Core library project file
├── SparklerNet.Samples/    # Sample application
│   ├── Profiles/           # Configuration profiles
│   │   ├── IProfile.cs     # Profile interface
│   │   ├── MimicApplicationProfile.cs # Mimic application profile
│   │   └── TCKApplicationProfile.cs # TCK application profile
│   ├── Program.cs          # Sample program entry point
│   ├── SimpleHostApplication.cs # Simple host application implementation
│   └── SparklerNet.Samples.csproj # Sample project file
├── SparklerNet.Tests/      # Unit tests
│   ├── Core/               # Core functionality tests
│   │   ├── Constants/      # Constant tests
│   │   ├── Extensions/     # Extension method tests
│   │   ├── Model/          # Model tests
│   │   └── Topics/         # Topic tests
│   ├── HostApplication/    # Host application tests
│   │   └── Caches/         # Cache mechanism tests
│   └── SparklerNet.Tests.csproj # Test project file
├── SparklerNet.sln         # Solution file
└── SparklerNet.sln.DotSettings # ReSharper settings
```

## Supported Sparkplug B Message Types

SparklerNet supports the following Sparkplug B message types:

- **NBIRTH**: Message sent when an edge node starts up
- **NDEATH**: Message sent when an edge node shuts down
- **NDATA**: Edge node data message
- **DBIRTH**: Message sent when a device starts up
- **DDEATH**: Message sent when a device shuts down
- **DDATA**: Device data message
- **NCMD**: Edge node command
- **DCMD**: Device command
- **STATE**: Host application state message

## Dependencies

- Google.Protobuf (3.33.0)
- Microsoft.Extensions.Caching.Hybrid (9.10.0)
- Microsoft.Extensions.Caching.Memory (9.0.10)
- Microsoft.Extensions.Logging (9.0.10)
- MQTTnet (5.0.1.1416)
- System.Net.Http (4.3.4)
- System.Text.RegularExpressions (4.3.1)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
