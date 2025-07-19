# RimAI Framework 开发者快速上手指南

## 开发环境准备

### 必需工具
- Visual Studio 2022 或 VS Code
- .NET Framework 4.8
- RimWorld 开发环境

### 项目结构理解
```
RimAI.Framework/
├── Source/
│   ├── API/                 # 公共API接口
│   │   └── RimAIApi.cs     # 主要API入口
│   ├── Core/               # 核心功能
│   │   ├── RimAIMod.cs     # Mod主类
│   │   └── RimAISettings.cs # 设置类
│   └── LLM/                # LLM相关功能
│       ├── LLMManager.cs   # 管理器
│       ├── Configuration/  # 配置管理
│       ├── Http/          # HTTP处理
│       ├── Models/        # 数据模型
│       ├── RequestQueue/  # 请求队列
│       └── Services/      # 各种服务
```

## 快速开发场景

### 场景1：添加一个新的AI功能服务

假设您想添加一个"智能翻译服务"：

#### Step 1: 创建服务接口
```csharp
// 在 Services/ILLMService.cs 中添加
public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string targetLanguage, TranslationOptions options = null);
    Task<TranslationResult> TranslateWithDetailsAsync(string text, string targetLanguage);
}

// 创建选项类
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

#### Step 2: 实现服务类
```csharp
// 创建 Services/TranslationService.cs
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
            var prompt = $"翻译以下文本到{targetLanguage}，并返回JSON格式包含翻译结果、检测到的源语言和置信度: {text}";
            
            var response = await jsonService.SendJsonRequestAsync<TranslationResult>(prompt);
            return response.Success ? response.Data : null;
        }

        private string BuildTranslationPrompt(string text, string targetLanguage, TranslationOptions options)
        {
            var promptBuilder = new System.Text.StringBuilder();
            promptBuilder.AppendLine($"请将以下文本翻译成{targetLanguage}：");
            
            if (options.SourceLanguage != "auto")
            {
                promptBuilder.AppendLine($"源语言：{options.SourceLanguage}");
            }
            
            promptBuilder.AppendLine($"待翻译文本：{text}");
            promptBuilder.AppendLine("只返回翻译结果，不要包含其他说明。");
            
            return promptBuilder.ToString();
        }
    }
}
```

#### Step 3: 在工厂中注册
```csharp
// 在 Services/LLMServiceFactory.cs 中添加
public ITranslationService CreateTranslationService(ILLMExecutor executor)
{
    return new TranslationService(executor, _settings);
}
```

#### Step 4: 在管理器中集成
```csharp
// 在 LLMManager.cs 中添加
public class LLMManager : IDisposable
{
    // 现有成员...
    private readonly ITranslationService _translationService;

    private LLMManager()
    {
        // 现有初始化...
        _translationService = _serviceFactory.CreateTranslationService(_executor);
    }

    // 属性暴露
    public ITranslationService TranslationService => _translationService;
}
```

#### Step 5: 在API中暴露
```csharp
// 在 API/RimAIApi.cs 中添加
public static class RimAIAPI
{
    // 现有方法...
    
    /// <summary>
    /// 获取翻译服务
    /// </summary>
    public static ITranslationService GetTranslationService()
    {
        return LLMManager.Instance?.TranslationService;
    }
}
```

#### Step 6: 使用示例
```csharp
// 在您的Mod中使用
public class MyTranslationMod : Mod
{
    private async void TranslateColonistName()
    {
        var translationService = RimAIAPI.GetTranslationService();
        if (translationService != null)
        {
            var result = await translationService.TranslateAsync(
                "Hello, World!", 
                "中文",
                new TranslationOptions 
                { 
                    IncludeConfidence = true,
                    Temperature = 0.3 // 低温度保证翻译准确性
                }
            );
            
            Log.Message($"翻译结果: {result}");
        }
    }
}
```

### 场景2：扩展现有服务功能

假设您想为JsonService添加批量处理功能：

#### Step 1: 扩展接口
```csharp
// 在 Services/ILLMService.cs 中的 IJsonLLMService 添加
public interface IJsonLLMService
{
    // 现有方法...
    
    // 新增批量处理方法
    Task<List<JsonResponse<T>>> SendBatchJsonRequestAsync<T>(
        List<string> prompts, 
        LLMRequestOptions options = null
    );
}
```

#### Step 2: 实现新功能
```csharp
// 在 Services/JsonLLMService.cs 中添加
public async Task<List<JsonResponse<T>>> SendBatchJsonRequestAsync<T>(
    List<string> prompts, 
    LLMRequestOptions options = null)
{
    var results = new List<JsonResponse<T>>();
    var semaphore = new SemaphoreSlim(3); // 限制并发数

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

### 场景3：添加新的请求预设

```csharp
// 在 API/RimAIApi.cs 的 Options 类中添加
public static class Options
{
    // 现有预设...
    
    /// <summary>
    /// 翻译专用预设
    /// </summary>
    public static LLMRequestOptions Translation(string sourceLanguage = "auto", string targetLanguage = "English")
    {
        return new LLMRequestOptions
        {
            Temperature = 0.3,  // 低温度保证准确性
            MaxTokens = 1000,
            AdditionalParameters = new Dictionary<string, object>
            {
                ["source_language"] = sourceLanguage,
                ["target_language"] = targetLanguage
            }
        };
    }
    
    /// <summary>
    /// 代码分析专用预设
    /// </summary>
    public static LLMRequestOptions CodeAnalysis(string programmingLanguage = "C#")
    {
        return new LLMRequestOptions
        {
            Temperature = 0.2,  // 极低温度保证精确性
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
    /// 故事创作专用预设
    /// </summary>
    public static LLMRequestOptions StoryTelling(string genre = "sci-fi", int maxLength = 3000)
    {
        return new LLMRequestOptions
        {
            Temperature = 1.1,   // 高创造性
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

## 常见开发模式

### 模式1：带缓存的服务

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

### 模式2：带重试机制的服务

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
                await Task.Delay(1000 * attempt); // 指数退避
            }
        }

        throw new InvalidOperationException($"Operation failed after {_maxRetries} attempts");
    }
}
```

### 模式3：事件驱动的服务

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

## 调试和测试技巧

### 1. 调试HTTP请求

```csharp
// 创建一个调试版本的HttpClient
public class DebugHttpClient : HttpClient
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // 记录请求
        Log.Message($"HTTP Request: {request.Method} {request.RequestUri}");
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            Log.Message($"Request Body: {content}");
        }

        var response = await base.SendAsync(request, cancellationToken);

        // 记录响应
        Log.Message($"HTTP Response: {response.StatusCode}");
        var responseContent = await response.Content.ReadAsStringAsync();
        Log.Message($"Response Body: {responseContent}");

        return response;
    }
}
```

### 2. 性能监控

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

### 3. 单元测试示例

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

## 部署和发布

### 1. 版本兼容性

```csharp
// 在您的服务中检查框架版本
public class MyService
{
    public MyService()
    {
        if (!RimAIAPI.IsInitialized)
        {
            throw new InvalidOperationException("RimAI Framework is not initialized");
        }
        
        // 检查是否有所需的服务
        if (RimAIAPI.GetJsonService() == null)
        {
            throw new InvalidOperationException("JsonService is not available in this version");
        }
    }
}
```

### 2. 配置验证

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

### 3. 错误恢复

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

## 最佳实践总结

1. **始终检查初始化状态**: 使用 `RimAIAPI.IsInitialized`
2. **优雅处理错误**: 网络请求可能失败，要有备用方案
3. **合理设置Temperature**: 根据用途选择合适的创造性级别
4. **使用适当的预设**: 不要每次都手动配置选项
5. **考虑性能**: 对于大量请求，使用批量处理或缓存
6. **版本兼容**: 检查所需服务是否可用
7. **监控和日志**: 记录关键操作用于调试
8. **资源清理**: 正确处理IDisposable对象

这个架构的设计让您可以专注于业务逻辑，而不需要关心底层的HTTP通信、队列管理等复杂性。通过这些模式和实践，您可以快速开发出高质量的AI集成功能。
