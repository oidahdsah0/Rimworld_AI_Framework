# RimAI Framework 架构设计文档

## 概述

RimAI Framework 经历了从单一 LLMManager 到模块化架构的演进。当前架构采用了**分层设计**和**依赖注入**模式，将原本的巨型类拆分成多个专门职责的组件。

## 整体架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                    RimAIAPI (Public Interface)                 │
└─────────────────────┬───────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────┐
│                  LLMManager (Coordinator)                      │
│  - 单例管理                                                     │
│  - 服务协调                                                     │  
│  - 生命周期管理                                                │
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

## 核心组件详解

### 1. **LLMManager.cs** - 协调者（Coordinator）

**职责**: 系统的入口点和协调中心
- ✅ **单例管理**: 确保全局只有一个实例
- ✅ **依赖协调**: 管理各个服务组件的生命周期
- ✅ **API统一**: 提供统一的公共接口
- ✅ **向后兼容**: 保持老版本API的兼容性

**关键依赖**:
```csharp
private readonly HttpClient _httpClient;
private readonly SettingsManager _settingsManager;
private readonly LLMServiceFactory _serviceFactory;
private readonly ILLMExecutor _executor;
private readonly LLMRequestQueue _requestQueue;
```

**设计模式**: 门面模式(Facade Pattern) + 单例模式

### 2. **Services Layer** - 服务层

#### 2.1 **LLMServiceFactory.cs** - 服务工厂

**职责**: 负责创建和配置所有LLM相关服务
- ✅ **依赖注入**: 统一管理依赖关系
- ✅ **对象创建**: 封装复杂的对象构造逻辑
- ✅ **配置管理**: 将设置传递给各个服务

**创建的服务**:
- `ILLMExecutor` - HTTP通信执行器
- `LLMRequestQueue` - 请求队列管理器
- `ICustomLLMService` - 自定义请求服务
- `IJsonLLMService` - JSON强制服务
- `IModService` - Mod集成服务

#### 2.2 **LLMExecutor.cs** - 执行引擎

**职责**: 处理实际的HTTP通信
- ✅ **HTTP通信**: 发送请求到LLM API
- ✅ **流式处理**: 支持Server-Sent Events
- ✅ **错误处理**: 统一的错误处理和重试
- ✅ **响应解析**: 解析API响应

**核心方法**:
```csharp
Task<string> ExecuteSingleRequestAsync(string prompt, CancellationToken ct);
Task ExecuteStreamingRequestAsync(string prompt, Action<string> onChunkReceived, CancellationToken ct);
Task<(bool success, string message)> TestConnectionAsync();
```

#### 2.3 **CustomLLMService.cs** - 自定义服务

**职责**: 提供完全控制的API调用能力
- ✅ **完全控制**: 用户可以设置所有OpenAI API参数
- ✅ **高级功能**: 支持function calling、response format等
- ✅ **灵活性**: 不受框架默认设置限制

#### 2.4 **JsonLLMService.cs** - JSON服务

**职责**: 强制返回有效JSON格式
- ✅ **格式保证**: 确保响应是有效JSON
- ✅ **类型安全**: 支持泛型反序列化
- ✅ **错误恢复**: JSON解析失败时的处理

#### 2.5 **ModService.cs** - Mod集成服务

**职责**: 为其他Mod提供专门的集成功能
- ✅ **Mod隔离**: 每个Mod有独立配置
- ✅ **系统提示**: 支持Mod特定的系统提示
- ✅ **参数覆盖**: 允许Mod级别的参数定制

### 3. **Configuration Layer** - 配置层

#### 3.1 **SettingsManager.cs** - 设置管理器

**职责**: 管理配置的加载和缓存
- ✅ **设置缓存**: 避免重复加载配置
- ✅ **线程安全**: 多线程环境下的安全访问
- ✅ **热重载**: 支持运行时设置更新

### 4. **RequestQueue Layer** - 请求队列层

#### 4.1 **LLMRequestQueue.cs** - 请求队列

**职责**: 管理请求的排队和并发控制
- ✅ **并发控制**: 限制同时进行的请求数量
- ✅ **队列管理**: FIFO请求处理
- ✅ **取消支持**: 支持请求取消和清理

#### 4.2 **RequestData.cs** - 请求数据

**职责**: 封装单个请求的所有信息
- ✅ **数据封装**: 统一的请求数据结构
- ✅ **流式支持**: 区分流式和非流式请求
- ✅ **生命周期**: 自动资源清理

### 5. **Models Layer** - 模型层

#### 5.1 **LLMRequest.cs** - 请求模型

**定义的类型**:
- `LLMRequest` - 基础请求结构
- `LLMRequestOptions` - 请求选项配置
- `CustomRequest` - 自定义请求结构
- `Message` - 消息结构

#### 5.2 **LLMResponse.cs** - 响应模型

**定义的类型**:
- `LLMResponse` - 基础响应结构
- `JsonResponse<T>` - 泛型JSON响应
- `CustomResponse` - 自定义响应结构

### 6. **Http Layer** - HTTP层

#### 6.1 **HttpClientFactory.cs** - HTTP客户端工厂

**职责**: 管理HTTP客户端的创建和配置
- ✅ **客户端管理**: 统一的HttpClient配置
- ✅ **连接复用**: 避免Socket耗尽问题
- ✅ **超时控制**: 统一的超时设置

## 数据流图

### 标准请求流程
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

### 流式请求流程
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

## 扩展开发指南

### 添加新服务类型

#### 1. 创建服务接口
```csharp
// 在 ILLMService.cs 中添加
public interface IMyNewService
{
    Task<string> ProcessAsync(string input, MyOptions options = null);
}
```

#### 2. 实现服务类
```csharp
// 创建 MyNewService.cs
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
        // 实现你的逻辑
        return await _executor.ExecuteSingleRequestAsync(input, CancellationToken.None);
    }
}
```

#### 3. 在工厂中注册
```csharp
// 在 LLMServiceFactory.cs 中添加
public IMyNewService CreateMyNewService(ILLMExecutor executor)
{
    return new MyNewService(executor, _settings);
}
```

#### 4. 在管理器中集成
```csharp
// 在 LLMManager.cs 中添加
private readonly IMyNewService _myNewService;

// 构造函数中
_myNewService = _serviceFactory.CreateMyNewService(_executor);

// 属性暴露
public IMyNewService MyNewService => _myNewService;
```

#### 5. 在API中暴露
```csharp
// 在 RimAIApi.cs 中添加
public static IMyNewService GetMyNewService()
{
    return LLMManager.Instance?.MyNewService;
}
```

### 添加新的请求选项

#### 1. 扩展选项类
```csharp
// 在 LLMRequest.cs 的 LLMRequestOptions 中添加
public class LLMRequestOptions
{
    // 现有属性...
    
    // 新属性
    public bool MyNewFeature { get; set; } = false;
    public double? MyCustomParameter { get; set; }
}
```

#### 2. 在执行器中处理
```csharp
// 在 LLMExecutor.cs 中修改请求构建逻辑
var requestBody = new
{
    model = _settings.modelName,
    messages = new[] { new { role = "user", content = prompt } },
    stream = false,
    temperature = _settings.temperature,
    // 添加新参数的处理
    my_custom_parameter = options?.MyCustomParameter
};
```

### 添加新的预设选项

```csharp
// 在 RimAIApi.cs 的 Options 类中添加
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

### 修改请求处理逻辑

#### 1. 添加新的请求类型
```csharp
// 在 RequestData.cs 中添加新构造函数
public RequestData(string prompt, MyCustomCallback customCallback, CancellationToken ct)
{
    Prompt = prompt;
    CustomCallback = customCallback;
    CancellationToken = ct;
    RequestType = RequestType.Custom;
}
```

#### 2. 在队列中处理新类型
```csharp
// 在 LLMRequestQueue.cs 中添加处理逻辑
private async Task ProcessCustomRequest(RequestData requestData)
{
    // 自定义处理逻辑
}
```

## 设计原则

### 1. **单一职责原则** (Single Responsibility Principle)
每个类只负责一个特定的功能：
- `LLMExecutor` 只负责HTTP通信
- `LLMRequestQueue` 只负责队列管理
- `SettingsManager` 只负责配置管理

### 2. **依赖倒置原则** (Dependency Inversion Principle)
高层模块不依赖低层模块，都依赖抽象：
- 使用接口 `ILLMExecutor`, `ICustomLLMService` 等
- 通过构造函数注入依赖

### 3. **开放封闭原则** (Open/Closed Principle)
对扩展开放，对修改封闭：
- 通过工厂模式添加新服务
- 通过策略模式支持不同的处理方式

### 4. **里氏替换原则** (Liskov Substitution Principle)
子类可以替换父类：
- 所有服务都实现统一接口
- 可以替换不同的实现

## 性能考虑

### 1. **对象池化**
- HttpClient 重用避免连接耗尽
- RequestData 对象的适当缓存

### 2. **异步处理**
- 所有IO操作使用 async/await
- 避免阻塞UI线程

### 3. **内存管理**
- 及时释放资源 (IDisposable)
- 避免内存泄漏

### 4. **并发控制**
- 使用 SemaphoreSlim 限制并发请求
- 线程安全的设计

## 测试策略

### 1. **单元测试**
每个服务独立测试：
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

### 2. **集成测试**
测试组件间协作：
```csharp
[Test]
public async Task LLMManager_Should_Coordinate_Services()
{
    // 测试完整的请求流程
}
```

### 3. **Mock 测试**
使用Mock对象隔离依赖：
```csharp
var mockExecutor = new Mock<ILLMExecutor>();
var service = new CustomLLMService(mockExecutor.Object, settings);
```

## 故障排查

### 1. **常见问题**
- **初始化失败**: 检查 `LLMManager.Instance` 是否为null
- **请求超时**: 检查网络连接和API配置
- **内存泄漏**: 确保 `IDisposable` 对象被正确释放

### 2. **调试技巧**
- 使用 `Log.Message()` 记录关键步骤
- 检查 `RimAIAPI.IsInitialized` 状态
- 使用 `TestConnectionAsync()` 验证API连接

### 3. **性能监控**
- 监控并发请求数量
- 跟踪响应时间
- 检查内存使用情况

---

这个架构的核心优势在于**模块化**、**可扩展性**和**可维护性**。每个组件都有明确的职责，新功能的添加不会影响现有代码，同时保持了良好的性能和稳定性。
