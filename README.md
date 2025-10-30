# SparklerNet

[![License](https://img.shields.io/github/license/longkerdandy/SparklerNet)](https://github.com/longkerdandy/SparklerNet/blob/main/LICENSE)
[![Language](https://img.shields.io/github/languages/top/longkerdandy/SparklerNet)](https://github.com/longkerdandy/SparklerNet)
[![.NET](https://github.com/longkerdandy/SparklerNet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/longkerdandy/SparklerNet/actions/workflows/dotnet.yml)
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

## Roadmap - Release v1.0.0

### Core Functionality

- ✅ Connection to MQTT brokers v3.1.1 or v5.0
- ✅ Connection using TCP, TCP+TLS and WebSockets
- ✅ Support for Sparkplug B data types (Int8/16/32/64, UInt8/16/32/64, Float, Double, Boolean, String, DateTime, Text, UUID, DataSet, Bytes, File, Template, PropertySet, PropertySetList)
- ✅ Support for Sparkplug B array data types (Int8Array, Int16Array, Int32Array, Int64Array, UInt8Array, UInt16Array, UInt32Array, UInt64Array, FloatArray, DoubleArray, BooleanArray, StringArray, DateTimeArray)

### Host Application

- ✅ Sparkplug-compliant Will message
- ✅ STATE message publishing (Birth/Death certificates)
- ✅ Deafult wildcard topic support (spBv1.0/#)
- ✅ Specific group and edge node subscription support
- ⬜ Sparkplug Host Application Message Ordering
- ⬜ Mapping between Metric's name and alias

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

- ⬜ Payload validation for all message types
- ⬜ Group ID and Edge Node ID validation

### Additional Features

- ⬜ Reconnection logic with exponential backoff
- ⬜ Configuration validation

## Installation

Install SparklerNet via NuGet Package Manager:

```bash
dotnet add package SparklerNet
```

Or reference it directly in your project:

```xml
<ItemGroup>
  <PackageReference Include="SparklerNet" Version="0.9.0" />
</ItemGroup>
```

## Quick Start

Here's a simple example of a Sparkplug host application:

```csharp
// Create MQTT client options
var mqttOptions = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .WithProtocolVersion(MqttProtocolVersion.V500)
    .Build();

// Create Sparkplug client options
var sparkplugOptions = new SparkplugClientOptions
{
    HostApplicationId = "MyHostApplication",
    Version = SparkplugVersion.V300
};

// Create logger
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});
var logger = loggerFactory.CreateLogger<SparkplugHostApplication>();

// Create and start host application
var hostApplication = new SparkplugHostApplication(mqttOptions, sparkplugOptions, logger);

// Subscribe to DBIRTH event
hostApplication.DeviceBirthReceivedAsync += args => {
    Console.WriteLine($"Received DBIRTH message from Group={args.GroupId}, Node={args.EdgeNodeId}, Device={args.DeviceId}");
    // Device birth message can be processed here
    return Task.CompletedTask;
};

// Start the application
await hostApplication.StartAsync();

// Example of sending a Device Rebirth command
var payload = new Payload
{
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    Metrics =
    {
        new Metric
        {
            Name = "Device Control/Rebirth",
            DateType = DataType.Boolean,
            Value = true
        }
    }
};
await hostApplication.PublishDeviceCommandMessageAsync("myGroup", "myEdgeNode", "myDevice", payload);

// Stop the application
await hostApplication.StopAsync();
```

## Sample Application

The project includes a comprehensive sample named `SimpleHostApplication` that demonstrates a complete Sparkplug Host Application implementation with the following features:

- **Interactive Command-Line Interface**: Provides a user-friendly console interface with commands for controlling the application lifecycle and sending commands
- **Complete Event Handling**: Demonstrates subscription and processing of all Sparkplug message types (NBIRTH, NDATA, NDEATH, DBIRTH, DDATA, DDEATH, STATE)
- **Robust Error Handling**: Includes comprehensive exception handling throughout the application lifecycle
- **Advanced Logging System**: Implements structured logging using Serilog, providing detailed information about message reception and processing
- **Command Sending Capabilities**: Allows sending rebirth commands to both Edge Nodes and Devices with customizable parameters
- **User-Friendly Input**: Features command prompts with default values for improved user experience
- **Detailed Data Display**: Shows comprehensive information about received messages including timestamps, sequences, and all metrics with their types and values

Please refer to the `SparklerNet.Samples` project for the complete implementation and to see these features in action.

## Project Structure

```
├── SparklerNet/            # Core library
│   ├── Core/               # Core functionality implementation
│   │   ├── Constants/      # Constant definitions
│   │   ├── Events/         # Event definitions
│   │   ├── Extensions/     # Extension methods
│   │   ├── Model/          # Data models
│   │   ├── Options/        # Configuration options
│   │   ├── Protobuf/       # Protobuf message definitions
│   │   └── Topics/         # Topic handling
│   └── HostApplication/    # Host application implementation
├── SparklerNet.Samples/    # Sample application
├── SparklerNet.Tests/      # Unit tests
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

- Google.Protobuf (3.32.1)
- Microsoft.Extensions.Logging (9.0.9)
- MQTTnet (5.0.1.1416)

## Contribution Guidelines

Contributions via Pull Requests and Issues are welcome. Before submitting code, please ensure:

1. Follow the project's code style and [Git Flow](GIT_FLOW.md)
2. Add necessary tests
3. Ensure all tests pass
4. Provide detailed code explanations

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
