# RimAI Framework Developer Quick Start Guide

## Development Environment Setup

### Required Tools
- Visual Studio 2022 or VS Code
- .NET Framework 4.8
- RimWorld development environment

### Project Structure Understanding
```
RimAI.Framework/
├── Source/
│   ├── API/                 # Public API interface
│   │   └── RimAIApi.cs     # Main API entry point
│   ├── Core/               # Core functionality
│   │   ├── RimAIMod.cs     # Main mod class
│   │   └── RimAISettings.cs # Settings class
│   └── LLM/                # LLM-related functionality
│       ├── LLMManager.cs   # Manager
│       ├── Configuration/  # Configuration management
│       ├── Http/          # HTTP handling
│       ├── Models/        # Data models
│       ├── RequestQueue/  # Request queue
│       └── Services/      # Various services
```

## Quick Development Scenarios

### Scenario 1: Adding a New AI Feature Service

Suppose you want to add an "Intelligent Translation Service":

#### Step 1: Create Service Interface
```csharp
// Add to Services/ILLMService.cs
public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string targetLanguage, TranslationOptions options = null);
    Task<TranslationResult> TranslateWithDetailsAsync(string text, string targetLanguage);
}

// Create options class
public class TranslationOptions : LLMRequestOptions
{
    public string SourceLanguage { get; set; } = "auto";
    public bool IncludeConfidence { get; set; } = false;
}

public class TranslationResult
{
    public string TranslatedText { get; set; }
    public string DetectedSourceLanguage { get; set; }
    public double Confidence { get; set; }
}
```

#### Step 2: Implement Service Class
```csharp
// Create Services/TranslationService.cs
using RimAI.Framework.LLM.Models;
using RimAI.Framework.Core;

namespace RimAI.Framework.LLM.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly ILLMExecutor _executor;
        private readonly RimAISettings _settings;

        public TranslationService(ILLMExecutor executor, RimAISettings settings)
        {
            _executor = executor;
            _settings = settings;
        }

        public async Task<string> TranslateAsync(string text, string targetLanguage, TranslationOptions options = null)
        {
            options ??= new TranslationOptions();
            
            var prompt = BuildTranslationPrompt(text, targetLanguage, options);
            return await _executor.ExecuteSingleRequestAsync(prompt, CancellationToken.None);
        }

        public async Task<TranslationResult> TranslateWithDetailsAsync(string text, string targetLanguage)
        {
            var jsonService = new JsonLLMService(_executor, _settings);
            var prompt = $"Translate the following text to {targetLanguage} and return JSON format including translation result, detected source language, and confidence: {text}";
            
            var response = await jsonService.SendJsonRequestAsync<TranslationResult>(prompt);
            return response.Success ? response.Data : null;
        }

        private string BuildTranslationPrompt(string text, string targetLanguage, TranslationOptions options)
        {
            var promptBuilder = new System.Text.StringBuilder();
            promptBuilder.AppendLine($"Please translate the following text to {targetLanguage}:");
            
            if (options.SourceLanguage != "auto")
            {
                promptBuilder.AppendLine($"Source language: {options.SourceLanguage}");
            }
            
            promptBuilder.AppendLine($"Text to translate: {text}");
            promptBuilder.AppendLine("Return only the translation result, no additional explanations.");
            
            return promptBuilder.ToString();
        }
    }
}
```

#### Step 3: Register in Factory
```csharp
// Add to Services/LLMServiceFactory.cs
public ITranslationService CreateTranslationService(ILLMExecutor executor)
{
    return new TranslationService(executor, _settings);
}
```

#### Step 4: Integrate in Manager
```csharp
// Add to LLMManager.cs
public class LLMManager : IDisposable
{
    // Existing members...
    private readonly ITranslationService _translationService;

    private LLMManager()
    {
        // Existing initialization...
        _translationService = _serviceFactory.CreateTranslationService(_executor);
    }

    // Property exposure
    public ITranslationService TranslationService => _translationService;
}
```

#### Step 5: Expose in API
```csharp
// Add to API/RimAIApi.cs
public static class RimAIAPI
{
    // Existing methods...
    
    /// <summary>
    /// Get translation service
    /// </summary>
    public static ITranslationService GetTranslationService()
    {
        return LLMManager.Instance?.TranslationService;
    }
}
```

#### Step 6: Usage Example
```csharp
// Usage in your mod
public class MyTranslationMod : Mod
{
    private async void TranslateColonistName()
    {
        var translationService = RimAIAPI.GetTranslationService();
        if (translationService != null)
        {
            var result = await translationService.TranslateAsync(
                "Hello, World!", 
                "Chinese",
                new TranslationOptions 
                { 
                    IncludeConfidence = true,
                    Temperature = 0.3 // Low temperature for translation accuracy
                }
            );
            
            Log.Message($"Translation result: {result}");
        }
    }
}
```

### Scenario 2: Extending Existing Service Functionality

Suppose you want to add batch processing capability to JsonService:

#### Step 1: Extend Interface
```csharp
// Add to Services/ILLMService.cs in IJsonLLMService
public interface IJsonLLMService
{
    // Existing methods...
    
    // New batch processing method
    Task<List<JsonResponse<T>>> SendBatchJsonRequestAsync<T>(
        List<string> prompts, 
        LLMRequestOptions options = null
    );
}
```

#### Step 2: Implement New Feature
```csharp
// Add to Services/JsonLLMService.cs
public async Task<List<JsonResponse<T>>> SendBatchJsonRequestAsync<T>(
    List<string> prompts, 
    LLMRequestOptions options = null)
{
    var results = new List<JsonResponse<T>>();
    var semaphore = new SemaphoreSlim(3); // Limit concurrency

    var tasks = prompts.Select(async prompt =>
    {
        await semaphore.WaitAsync();
        try
        {
            return await SendJsonRequestAsync<T>(prompt, options);
        }
        finally
        {
            semaphore.Release();
        }
    });

    var responses = await Task.WhenAll(tasks);
    return responses.ToList();
}
```

### Scenario 3: Adding New Request Presets

```csharp
// Add to API/RimAIApi.cs in Options class
public static class Options
{
    // Existing presets...
    
    /// <summary>
    /// Translation-specific preset
    /// </summary>
    public static LLMRequestOptions Translation(string sourceLanguage = "auto", string targetLanguage = "English")
    {
        return new LLMRequestOptions
        {
            Temperature = 0.3,  // Low temperature for accuracy
            MaxTokens = 1000,
            AdditionalParameters = new Dictionary<string, object>
            {
                ["source_language"] = sourceLanguage,
                ["target_language"] = targetLanguage
            }
        };
    }
    
    /// <summary>
    /// Code analysis-specific preset
    /// </summary>
    public static LLMRequestOptions CodeAnalysis(string programmingLanguage = "C#")
    {
        return new LLMRequestOptions
        {
            Temperature = 0.2,  // Very low temperature for precision
            MaxTokens = 2000,
            ForceJsonMode = true,
            AdditionalParameters = new Dictionary<string, object>
            {
                ["language"] = programmingLanguage,
                ["analysis_depth"] = "detailed"
            }
        };
    }
    
    /// <summary>
    /// Storytelling-specific preset
    /// </summary>
    public static LLMRequestOptions StoryTelling(string genre = "sci-fi", int maxLength = 3000)
    {
        return new LLMRequestOptions
        {
            Temperature = 1.1,   // High creativity
            MaxTokens = maxLength,
            EnableStreaming = true,
            AdditionalParameters = new Dictionary<string, object>
            {
                ["genre"] = genre,
                ["style"] = "engaging"
            }
        };
    }
}
```

## Common Development Patterns

### Pattern 1: Service with Caching

```csharp
public class CachedTranslationService : ITranslationService
{
    private readonly ITranslationService _innerService;
    private readonly Dictionary<string, string> _cache = new();

    public CachedTranslationService(ITranslationService innerService)
    {
        _innerService = innerService;
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage, TranslationOptions options = null)
    {
        var key = $"{text}:{targetLanguage}";
        
        if (_cache.ContainsKey(key))
        {
            return _cache[key];
        }

        var result = await _innerService.TranslateAsync(text, targetLanguage, options);
        _cache[key] = result;
        
        return result;
    }
}
```

### Pattern 2: Service with Retry Mechanism

```csharp
public class ResilientService<T> where T : class
{
    private readonly T _innerService;
    private readonly int _maxRetries;

    public ResilientService(T innerService, int maxRetries = 3)
    {
        _innerService = innerService;
        _maxRetries = maxRetries;
    }

    public async Task<TResult> ExecuteWithRetry<TResult>(Func<T, Task<TResult>> operation)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation(_innerService);
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                Log.Warning($"Attempt {attempt} failed: {ex.Message}. Retrying...");
                await Task.Delay(1000 * attempt); // Exponential backoff
            }
        }

        throw new InvalidOperationException($"Operation failed after {_maxRetries} attempts");
    }
}
```

### Pattern 3: Event-Driven Service

```csharp
public class EventDrivenService : ICustomLLMService
{
    public event EventHandler<RequestStartedEventArgs> RequestStarted;
    public event EventHandler<RequestCompletedEventArgs> RequestCompleted;
    public event EventHandler<RequestFailedEventArgs> RequestFailed;

    private readonly ICustomLLMService _innerService;

    public async Task<CustomResponse> SendCustomRequestAsync(CustomRequest request)
    {
        var eventArgs = new RequestStartedEventArgs { Request = request };
        RequestStarted?.Invoke(this, eventArgs);

        try
        {
            var response = await _innerService.SendCustomRequestAsync(request);
            RequestCompleted?.Invoke(this, new RequestCompletedEventArgs 
            { 
                Request = request, 
                Response = response 
            });
            return response;
        }
        catch (Exception ex)
        {
            RequestFailed?.Invoke(this, new RequestFailedEventArgs 
            { 
                Request = request, 
                Exception = ex 
            });
            throw;
        }
    }
}
```

## Debugging and Testing Techniques

### 1. Debugging HTTP Requests

```csharp
// Create a debug version of HttpClient
public class DebugHttpClient : HttpClient
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // Log request
        Log.Message($"HTTP Request: {request.Method} {request.RequestUri}");
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            Log.Message($"Request Body: {content}");
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Log response
        Log.Message($"HTTP Response: {response.StatusCode}");
        var responseContent = await response.Content.ReadAsStringAsync();
        Log.Message($"Response Body: {responseContent}");

        return response;
    }
}
```

### 2. Performance Monitoring

```csharp
public class PerformanceMonitoringService<T> where T : class
{
    private readonly T _innerService;

    public async Task<TResult> MonitorPerformance<TResult>(
        Func<T, Task<TResult>> operation, 
        string operationName)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var result = await operation(_innerService);
            stopwatch.Stop();
            
            Log.Message($"{operationName} completed in {stopwatch.ElapsedMilliseconds}ms");
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Log.Error($"{operationName} failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
            throw;
        }
    }
}
```

### 3. Unit Testing Example

```csharp
[TestClass]
public class TranslationServiceTests
{
    private Mock<ILLMExecutor> _mockExecutor;
    private RimAISettings _settings;
    private TranslationService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockExecutor = new Mock<ILLMExecutor>();
        _settings = new RimAISettings();
        _service = new TranslationService(_mockExecutor.Object, _settings);
    }

    [TestMethod]
    public async Task TranslateAsync_ShouldReturnTranslation()
    {
        // Arrange
        _mockExecutor.Setup(x => x.ExecuteSingleRequestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("Hello, World!");

        // Act
        var result = await _service.TranslateAsync("你好，世界！", "English");

        // Assert
        Assert.AreEqual("Hello, World!", result);
        _mockExecutor.Verify(x => x.ExecuteSingleRequestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## Deployment and Publishing

### 1. Version Compatibility

```csharp
// Check framework version in your service
public class MyService
{
    public MyService()
    {
        if (!RimAIAPI.IsInitialized)
        {
            throw new InvalidOperationException("RimAI Framework is not initialized");
        }
        
        // Check if required services are available
        if (RimAIAPI.GetJsonService() == null)
        {
            throw new InvalidOperationException("JsonService is not available in this version");
        }
    }
}
```

### 2. Configuration Validation

```csharp
public static class ConfigurationValidator
{
    public static bool ValidateConfiguration()
    {
        var settings = RimAIAPI.CurrentSettings;
        
        if (string.IsNullOrEmpty(settings?.apiKey))
        {
            Log.Error("API Key is not configured");
            return false;
        }

        if (string.IsNullOrEmpty(settings?.apiEndpoint))
        {
            Log.Error("API Endpoint is not configured");
            return false;
        }

        return true;
    }
}
```

### 3. Error Recovery

```csharp
public class ResilientAIService
{
    public async Task<string> SafeProcessAsync(string input)
    {
        try
        {
            if (!ConfigurationValidator.ValidateConfiguration())
            {
                return "AI service is not properly configured.";
            }

            var result = await RimAIAPI.SendMessageAsync(input);
            return result ?? "AI service is temporarily unavailable.";
        }
        catch (Exception ex)
        {
            Log.Error($"AI processing failed: {ex.Message}");
            return "An error occurred while processing your request.";
        }
    }
}
```

## Best Practices Summary

1. **Always check initialization status**: Use `RimAIAPI.IsInitialized`
2. **Graceful error handling**: Network requests may fail, have backup plans
3. **Set appropriate Temperature**: Choose suitable creativity levels based on use case
4. **Use appropriate presets**: Don't manually configure options every time
5. **Consider performance**: For large numbers of requests, use batch processing or caching
6. **Version compatibility**: Check if required services are available
7. **Monitoring and logging**: Record key operations for debugging
8. **Resource cleanup**: Properly handle IDisposable objects

This architecture design allows you to focus on business logic without worrying about the complexities of underlying HTTP communication, queue management, etc. Through these patterns and practices, you can quickly develop high-quality AI integration features.
