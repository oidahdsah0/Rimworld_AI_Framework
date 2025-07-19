# RimAI Framework Architecture Design Document

## Overview

RimAI Framework has evolved from a single LLMManager to a modular architecture. The current architecture adopts **layered design** and **dependency injection** patterns, splitting the original monolithic class into multiple components with specific responsibilities.

## Overall Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    RimAIAPI (Public Interface)                 │
└─────────────────────┬───────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────┐
│                  LLMManager (Coordinator)                      │
│  - Singleton Management                                        │
│  - Service Coordination                                        │  
│  - Lifecycle Management                                        │
└─────┬───────┬───────┬───────┬───────────────┬─────────────────────┘
      │       │       │       │               │
      ▼       ▼       ▼       ▼               ▼
┌──────────┐ ┌──────┐ ┌──────┐ ┌─────────┐ ┌──────────────┐
│HttpClient│ │Config│ │Queue │ │Executor │ │ServiceFactory│
└──────────┘ └──────┘ └──────┘ └─────────┘ └──────────────┘
      │         │       │       │               │
      └─────────┼───────┼───────┼───────────────┘
                │       │       │
                ▼       ▼       ▼
        ┌─────────────────────────────────────┐
        │           Services Layer            │
        │  ┌─────────────┐ ┌───────────────┐  │
        │  │CustomService│ │JsonService    │  │
        │  └─────────────┘ └───────────────┘  │
        │  ┌─────────────┐ ┌───────────────┐  │
        │  │ModService   │ │LLMExecutor    │  │
        │  └─────────────┘ └───────────────┘  │
        └─────────────────────────────────────┘
```

## Core Component Details

### 1. **LLMManager.cs** - Coordinator

**Responsibilities**: System entry point and coordination center
- ✅ **Singleton Management**: Ensures only one global instance
- ✅ **Dependency Coordination**: Manages lifecycle of all service components
- ✅ **API Unification**: Provides unified public interface
- ✅ **Backward Compatibility**: Maintains compatibility with older API versions

**Key Dependencies**:
```csharp
private readonly HttpClient _httpClient;
private readonly SettingsManager _settingsManager;
private readonly LLMServiceFactory _serviceFactory;
private readonly ILLMExecutor _executor;
private readonly LLMRequestQueue _requestQueue;
```

**Design Patterns**: Facade Pattern + Singleton Pattern

### 2. **Services Layer**

#### 2.1 **LLMServiceFactory.cs** - Service Factory

**Responsibilities**: Creates and configures all LLM-related services
- ✅ **Dependency Injection**: Centrally manages dependency relationships
- ✅ **Object Creation**: Encapsulates complex object construction logic
- ✅ **Configuration Management**: Passes settings to various services

**Created Services**:
- `ILLMExecutor` - HTTP communication executor
- `LLMRequestQueue` - Request queue manager
- `ICustomLLMService` - Custom request service
- `IJsonLLMService` - JSON enforcement service
- `IModService` - Mod integration service

#### 2.2 **LLMExecutor.cs** - Execution Engine

**Responsibilities**: Handles actual HTTP communication
- ✅ **HTTP Communication**: Sends requests to LLM API
- ✅ **Streaming Processing**: Supports Server-Sent Events
- ✅ **Error Handling**: Unified error handling and retry logic
- ✅ **Response Parsing**: Parses API responses

**Core Methods**:
```csharp
Task<string> ExecuteSingleRequestAsync(string prompt, CancellationToken ct);
Task ExecuteStreamingRequestAsync(string prompt, Action<string> onChunkReceived, CancellationToken ct);
Task<(bool success, string message)> TestConnectionAsync();
```

#### 2.3 **CustomLLMService.cs** - Custom Service

**Responsibilities**: Provides full control API calling capability
- ✅ **Complete Control**: Users can set all OpenAI API parameters
- ✅ **Advanced Features**: Supports function calling, response format, etc.
- ✅ **Flexibility**: Not limited by framework default settings

#### 2.4 **JsonLLMService.cs** - JSON Service

**Responsibilities**: Enforces valid JSON format returns
- ✅ **Format Guarantee**: Ensures responses are valid JSON
- ✅ **Type Safety**: Supports generic deserialization
- ✅ **Error Recovery**: Handles JSON parsing failures

#### 2.5 **ModService.cs** - Mod Integration Service

**Responsibilities**: Provides specialized integration features for other mods
- ✅ **Mod Isolation**: Each mod has independent configuration
- ✅ **System Prompts**: Supports mod-specific system prompts
- ✅ **Parameter Override**: Allows mod-level parameter customization

### 3. **Configuration Layer**

#### 3.1 **SettingsManager.cs** - Settings Manager

**Responsibilities**: Manages configuration loading and caching
- ✅ **Settings Caching**: Avoids repeated configuration loading
- ✅ **Thread Safety**: Safe access in multi-threaded environments
- ✅ **Hot Reload**: Supports runtime settings updates

### 4. **RequestQueue Layer**

#### 4.1 **LLMRequestQueue.cs** - Request Queue

**Responsibilities**: Manages request queuing and concurrency control
- ✅ **Concurrency Control**: Limits number of simultaneous requests
- ✅ **Queue Management**: FIFO request processing
- ✅ **Cancellation Support**: Supports request cancellation and cleanup

#### 4.2 **RequestData.cs** - Request Data

**Responsibilities**: Encapsulates all information for a single request
- ✅ **Data Encapsulation**: Unified request data structure
- ✅ **Streaming Support**: Distinguishes streaming and non-streaming requests
- ✅ **Lifecycle**: Automatic resource cleanup

### 5. **Models Layer**

#### 5.1 **LLMRequest.cs** - Request Model

**Defined Types**:
- `LLMRequest` - Basic request structure
- `LLMRequestOptions` - Request options configuration
- `CustomRequest` - Custom request structure
- `Message` - Message structure

#### 5.2 **LLMResponse.cs** - Response Model

**Defined Types**:
- `LLMResponse` - Basic response structure
- `JsonResponse<T>` - Generic JSON response
- `CustomResponse` - Custom response structure

### 6. **Http Layer**

#### 6.1 **HttpClientFactory.cs** - HTTP Client Factory

**Responsibilities**: Manages HTTP client creation and configuration
- ✅ **Client Management**: Unified HttpClient configuration
- ✅ **Connection Reuse**: Avoids socket exhaustion issues
- ✅ **Timeout Control**: Unified timeout settings

## Data Flow Diagrams

### Standard Request Flow
```
User Request
    │
    ▼
RimAIAPI.SendMessageAsync()
    │
    ▼
LLMManager.SendMessageAsync()
    │
    ▼
LLMRequestQueue.EnqueueRequest()
    │
    ▼
LLMExecutor.ExecuteSingleRequestAsync()
    │
    ▼
HttpClient.SendAsync()
    │
    ▼
Parse Response
    │
    ▼
Return to User
```

### Streaming Request Flow
```
User Request + Callback
    │
    ▼
RimAIAPI.SendStreamingMessageAsync()
    │
    ▼
LLMManager.SendMessageStreamAsync()
    │
    ▼
LLMRequestQueue.EnqueueRequest()
    │
    ▼
LLMExecutor.ExecuteStreamingRequestAsync()
    │
    ▼ (For each chunk)
HttpClient.SendAsync() → Parse SSE → Callback
```

## Extension Development Guide

### Adding New Service Types

#### 1. Create Service Interface
```csharp
// Add to ILLMService.cs
public interface IMyNewService
{
    Task<string> ProcessAsync(string input, MyOptions options = null);
}
```

#### 2. Implement Service Class
```csharp
// Create MyNewService.cs
public class MyNewService : IMyNewService
{
    private readonly ILLMExecutor _executor;
    private readonly RimAISettings _settings;

    public MyNewService(ILLMExecutor executor, RimAISettings settings)
    {
        _executor = executor;
        _settings = settings;
    }

    public async Task<string> ProcessAsync(string input, MyOptions options = null)
    {
        // Implement your logic
        return await _executor.ExecuteSingleRequestAsync(input, CancellationToken.None);
    }
}
```

#### 3. Register in Factory
```csharp
// Add to LLMServiceFactory.cs
public IMyNewService CreateMyNewService(ILLMExecutor executor)
{
    return new MyNewService(executor, _settings);
}
```

#### 4. Integrate in Manager
```csharp
// Add to LLMManager.cs
private readonly IMyNewService _myNewService;

// In constructor
_myNewService = _serviceFactory.CreateMyNewService(_executor);

// Property exposure
public IMyNewService MyNewService => _myNewService;
```

#### 5. Expose in API
```csharp
// Add to RimAIApi.cs
public static IMyNewService GetMyNewService()
{
    return LLMManager.Instance?.MyNewService;
}
```

### Adding New Request Options

#### 1. Extend Options Class
```csharp
// Add to LLMRequestOptions in LLMRequest.cs
public class LLMRequestOptions
{
    // Existing properties...
    
    // New properties
    public bool MyNewFeature { get; set; } = false;
    public double? MyCustomParameter { get; set; }
}
```

#### 2. Handle in Executor
```csharp
// Modify request building logic in LLMExecutor.cs
var requestBody = new
{
    model = _settings.modelName,
    messages = new[] { new { role = "user", content = prompt } },
    stream = false,
    temperature = _settings.temperature,
    // Add handling for new parameter
    my_custom_parameter = options?.MyCustomParameter
};
```

### Adding New Preset Options

```csharp
// Add to Options class in RimAIApi.cs
public static LLMRequestOptions MyNewPreset(double? customParam = null)
{
    return new LLMRequestOptions
    {
        MyNewFeature = true,
        MyCustomParameter = customParam ?? 1.0,
        Temperature = 0.8
    };
}
```

### Modifying Request Processing Logic

#### 1. Add New Request Type
```csharp
// Add new constructor to RequestData.cs
public RequestData(string prompt, MyCustomCallback customCallback, CancellationToken ct)
{
    Prompt = prompt;
    CustomCallback = customCallback;
    CancellationToken = ct;
    RequestType = RequestType.Custom;
}
```

#### 2. Handle New Type in Queue
```csharp
// Add processing logic to LLMRequestQueue.cs
private async Task ProcessCustomRequest(RequestData requestData)
{
    // Custom processing logic
}
```

## Design Principles

### 1. **Single Responsibility Principle**
Each class is responsible for only one specific function:
- `LLMExecutor` handles only HTTP communication
- `LLMRequestQueue` handles only queue management
- `SettingsManager` handles only configuration management

### 2. **Dependency Inversion Principle**
High-level modules don't depend on low-level modules; both depend on abstractions:
- Use interfaces `ILLMExecutor`, `ICustomLLMService`, etc.
- Inject dependencies through constructors

### 3. **Open/Closed Principle**
Open for extension, closed for modification:
- Add new services through factory pattern
- Support different processing methods through strategy pattern

### 4. **Liskov Substitution Principle**
Subclasses can replace parent classes:
- All services implement unified interfaces
- Different implementations can be substituted

## Performance Considerations

### 1. **Object Pooling**
- HttpClient reuse to avoid connection exhaustion
- Appropriate caching of RequestData objects

### 2. **Asynchronous Processing**
- All IO operations use async/await
- Avoid blocking the UI thread

### 3. **Memory Management**
- Timely resource disposal (IDisposable)
- Avoid memory leaks

### 4. **Concurrency Control**
- Use SemaphoreSlim to limit concurrent requests
- Thread-safe design

## Testing Strategy

### 1. **Unit Testing**
Test each service independently:
```csharp
[Test]
public async Task LLMExecutor_Should_Handle_Request()
{
    // Arrange
    var mockHttpClient = new Mock<HttpClient>();
    var settings = new RimAISettings();
    var executor = new LLMExecutor(mockHttpClient.Object, settings);
    
    // Act & Assert
}
```

### 2. **Integration Testing**
Test component collaboration:
```csharp
[Test]
public async Task LLMManager_Should_Coordinate_Services()
{
    // Test complete request flow
}
```

### 3. **Mock Testing**
Use Mock objects to isolate dependencies:
```csharp
var mockExecutor = new Mock<ILLMExecutor>();
var service = new CustomLLMService(mockExecutor.Object, settings);
```

## Troubleshooting

### 1. **Common Issues**
- **Initialization Failure**: Check if `LLMManager.Instance` is null
- **Request Timeout**: Check network connection and API configuration
- **Memory Leaks**: Ensure `IDisposable` objects are properly disposed

### 2. **Debugging Techniques**
- Use `Log.Message()` to record key steps
- Check `RimAIAPI.IsInitialized` status
- Use `TestConnectionAsync()` to verify API connection

### 3. **Performance Monitoring**
- Monitor concurrent request count
- Track response times
- Check memory usage

---

The core advantages of this architecture lie in **modularity**, **extensibility**, and **maintainability**. Each component has clear responsibilities, adding new features doesn't affect existing code, while maintaining good performance and stability.
