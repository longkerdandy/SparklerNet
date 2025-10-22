# SparklerNet

[![.NET](https://github.com/longkerdandy/SparklerNet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/longkerdandy/SparklerNet/actions/workflows/dotnet.yml)
[![CodeQL Advanced](https://github.com/longkerdandy/SparklerNet/actions/workflows/codeql.yml/badge.svg)](https://github.com/longkerdandy/SparklerNet/actions/workflows/codeql.yml)

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

## Roadmap (1.0.0 Release)

### Core Functionality

- ✅ MQTT connection with Sparkplug-compliant Will message
- ✅ STATE message publishing (Birth/Death certificates)
- ✅ MQTT topic subscription management
- ✅ Wildcard topic support (spBv1.0/#)
- ✅ Specific group and edge node subscription support
- ✅ Message parsing and event handling
- ✅ Disconnect handling and reconnection events

### Message Processing

- ✅ Edge Node message handling (NBIRTH, NDEATH, NDATA)
- ✅ Device message handling (DBIRTH, DDEATH, DDATA)
- ✅ STATE message processing
- ✅ Unsupported message type handling

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
- ⬜ Message persistence
- ⬜ Metrics collection
- ⬜ Enhanced logging capabilities
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
var mqttFactory = new MqttClientFactory();
var mqttOptions = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .WithProtocolVersion(MqttProtocolVersion.V500)
    .Build();

// Create Sparkplug client options
var sparkplugOptions = new SparkplugClientOptions
{
    HostApplicationId = "MyHostApplication"
};

// Create logger
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});
var logger = loggerFactory.CreateLogger<SparkplugHostApplication>();

// Create and start host application
var hostApplication = new SparkplugHostApplication(mqttOptions, sparkplugOptions, logger);

// Subscribe to events
SubscribeToEvents(hostApplication);

// Start the application
await hostApplication.StartAsync();

// Example of sending a command
var payload = new Payload();
// Configure payload...
await hostApplication.PublishDeviceCommandMessageAsync("myGroup", "myEdgeNode", "myDevice", payload);
```

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

## Sample Application

The project includes a sample named `SimpleHostApplication` that demonstrates how to initialize, connect, receive messages, and send commands. Please refer to the `SparklerNet.Samples` project for more details.

## Dependencies

- Google.Protobuf (3.32.1)
- Microsoft.Extensions.Logging (9.0.9)
- MQTTnet (5.0.1.1416)
- JetBrains.Annotations (2025.2.2)

## Contribution Guidelines

Contributions via Pull Requests and Issues are welcome. Before submitting code, please ensure:

1. Follow the project's code style
2. Add necessary tests
3. Ensure all tests pass
4. Provide detailed code explanations

## License

[MIT](LICENSE)

## Acknowledgements

The SparklerNet project is inspired by the Eclipse Sparkplug specification and aims to provide .NET developers with an easy-to-use implementation of the Sparkplug protocol.
