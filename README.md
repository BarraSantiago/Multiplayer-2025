# MultiplayerLib

A comprehensive C# networking library for real-time multiplayer games, built with Unity-like architecture and featuring advanced synchronization capabilities.

## Features

### Core Networking
- **UDP-based Communication**: Reliable message delivery with acknowledgment system
- **Message Sequencing**: Ensures proper message ordering with sequence tracking
- **Automatic Resending**: Failed message detection and automatic retransmission
- **Client-Server Architecture**: Dedicated server model with matchmaking support

### Object Synchronization
- **NetworkObjectTracker**: Advanced reflection-based object state synchronization
- **Field-level Deltas**: Efficient transmission of only changed fields
- **Automatic Serialization**: Smart serialization of complex object hierarchies
- **Remote Method Calls**: RPC system with parameter serialization

### Game Features
- **Turn-based Game Support**: Built-in turn management system
- **Player Input Handling**: Structured input processing and validation
- **ELO Rating System**: Integrated player ranking with matchmaking
- **Real-time Ping Monitoring**: Network latency tracking and display

## Architecture

### Network Layer
```
AbstractNetworkManager
├── ClientNetworkManager
└── ServerNetworkManager
    ├── ClientManager
    ├── MessageDispatcher
    └── NetworkObjectTracker
```

### Message System
- **MessageEnvelope**: Secure message wrapper with encryption support
- **MessageTracker**: Reliable delivery tracking
- **TypedMessages**: Strongly-typed message serialization

### Object Management
- **NetworkObjectFactory**: Centralized object creation and management
- **INetworkObject**: Interface for networkable game objects
- **Reflection-based Sync**: Automatic field detection and synchronization

## Quick Start

### Server Setup
```csharp
var server = ServerNetworkManager.Instance;
server.StartServer(7777);
server.SetMatchmakerInfo(matchmakerIP, matchmakerPort, serverId);
```

### Client Connection
```csharp
var client = ClientNetworkManager.Instance;
client.ConnectToServer(serverIP, serverPort, playerName);
```

### Object Synchronization
```csharp
// Register an object for synchronization
networkTracker.RegisterObject(gameObject, objectId);

// Objects with [NetworkField] attributes are automatically synchronized
public class GameUnit : INetworkObject
{
    [NetworkField] public Vector3 Position { get; set; }
    [NetworkField] public int Health { get; set; }
    
    [NetworkMethod]
    public void TakeDamage(int damage)
    {
        Health -= damage;
    }
}
```

## Message Types

- **HandShake/HandShakeResponse**: Initial connection establishment
- **ObjectCreate/ObjectDestroy**: Network object lifecycle
- **ObjectUpdate**: Incremental state synchronization
- **PlayerInput**: Game action commands
- **MethodInvocation**: Remote procedure calls
- **GameResult**: Match outcome with ELO updates

## Key Components

### NetworkObjectTracker
Handles automatic synchronization of registered objects using reflection:
- Field change detection with hash comparison
- Efficient delta serialization
- Remote method invocation support
- Nested object synchronization

### MessageDispatcher
Routes and processes network messages:
- Type-safe message handling
- Acknowledgment system
- Sequence validation
- Automatic retransmission

### ClientManager
Server-side client state management:
- Connection tracking
- Timeout detection
- Player information storage
- Activity monitoring

## Security Features

- **Message Encryption**: Optional security seed-based message protection
- **Input Validation**: Server-side validation of all player actions
- **Anti-cheat**: State verification and correction system
- **Timeout Protection**: Automatic disconnection of inactive clients

## Performance Optimizations

- **Delta Compression**: Only changed fields are transmitted
- **Member Caching**: Reflection results cached for performance
- **Batched Updates**: Multiple changes sent in single messages
- **Lazy Serialization**: Objects serialized only when changed

## Dependencies

- .NET Framework/Core compatible
- System.Numerics for Vector3 support
- System.Reflection for automatic synchronization
- HarmonyLib for runtime patching (optional)

## License

This project is available under standard software licensing terms. Please refer to the license file for specific details.

## Contributing

Contributions are welcome! Please ensure all code follows the established patterns and includes appropriate error handling and logging.
