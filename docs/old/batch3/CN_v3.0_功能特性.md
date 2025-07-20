# RimAI Framework v3.0 功能特性

## 🎯 概述

RimAI Framework v3.0 引入了全新的统一架构，提供强大的AI功能集成和开发者友好的API体验。

## ✨ 核心新功能

### 1. 🏗️ 统一架构系统

#### 生命周期管理器 (LifecycleManager)
- **应用级资源管理**：统一管理所有框架组件的生命周期
- **自动健康检查**：每5分钟自动检查系统健康状态
- **优雅关闭支持**：确保所有正在进行的请求完成后再关闭
- **内存监控**：自动监控内存使用并触发垃圾回收

```csharp
// 系统会自动管理，无需手动调用
// 可选：手动获取健康状态
var isHealthy = LifecycleManager.Instance.IsHealthy;
```

#### 响应缓存系统 (ResponseCache)
- **LRU算法缓存**：智能管理缓存条目，自动清理过期内容
- **缓存统计监控**：实时跟踪命中率、内存使用等指标
- **智能缓存策略**：自动判断请求是否适合缓存

```csharp
// 启用缓存（默认已启用）
var options = new LLMRequestOptions { EnableCaching = true };
var response = await RimAIAPI.SendMessageAsync("Hello", options);

// 获取缓存统计
var stats = ResponseCache.Instance.GetStats();
Log.Message($"缓存命中率: {stats.HitRate:P2}");
```

#### 配置管理系统 (RimAIConfiguration)
- **集中配置管理**：统一管理所有框架配置
- **JSON文件持久化**：自动保存和加载配置
- **类型安全访问**：支持强类型配置获取

```csharp
// 获取配置值
var timeout = RimAIConfiguration.Instance.Get<int>("HTTP.TimeoutSeconds", 30);
var cacheSize = RimAIConfiguration.Instance.Get<int>("Cache.MaxSize", 100);
```

#### 连接池管理器 (ConnectionPoolManager)
- **HTTP连接追踪**：监控所有活跃的HTTP连接
- **自动清理机制**：清理过期和无效的连接
- **连接健康监控**：实时监控连接状态

```csharp
// 系统自动管理，可获取统计信息
var pool = ConnectionPoolManager.Instance;
Log.Message($"活跃连接数: {pool.ActiveConnectionCount}");
Log.Message($"健康连接数: {pool.HealthyConnectionCount}");
```

### 2. 📊 诊断和监控系统

#### 框架健康检查
```csharp
// 执行完整的健康检查
var healthResult = FrameworkDiagnostics.PerformHealthCheck();
if (!healthResult.IsHealthy)
{
    Log.Warning($"系统状态: {healthResult.Status}");
    foreach (var issue in healthResult.Issues)
    {
        Log.Error($"问题: {issue}");
    }
}
```

#### 性能监控报告
```csharp
// 生成性能报告
var report = FrameworkDiagnostics.GeneratePerformanceReport();
Log.Message(report.Summary);

// 查看推荐建议
foreach (var recommendation in report.Recommendations)
{
    Log.Message($"建议: {recommendation}");
}
```

### 3. 🔧 增强的异常处理系统

#### 结构化异常层次
- **RimAIException**：基础异常类，包含错误代码和上下文信息
- **LLMException**：LLM服务相关异常，支持工厂方法创建
- **ConnectionException**：网络连接异常，包含重试和超时信息
- **ConfigurationException**：配置相关异常，包含文件路径和验证错误

```csharp
try
{
    var response = await RimAIAPI.SendMessageAsync("Hello");
}
catch (LLMException ex)
{
    Log.Error($"LLM服务错误 [{ex.ErrorCode}]: {ex.Message}");
    if (ex.IsRecoverable)
    {
        // 可以重试的错误
    }
}
catch (ConnectionException ex)
{
    Log.Error($"连接错误: {ex.Message}, 重试次数: {ex.RetryCount}");
}
```

## 📚 API使用指南

### 基础消息发送
```csharp
// 最简单的使用方式
var response = await RimAIAPI.SendMessageAsync("你好，RimWorld！");

// 带参数的请求
var options = new LLMRequestOptions 
{
    Temperature = 0.7,
    MaxTokens = 500,
    Model = "gpt-3.5-turbo"
};
var response = await RimAIAPI.SendMessageAsync("生成一个RimWorld角色", options);
```

### 流式响应处理
```csharp
// 流式接收响应
await RimAIAPI.SendMessageStreamAsync(
    "讲一个RimWorld的故事",
    chunk => Log.Message($"接收: {chunk}"),
    new LLMRequestOptions { Temperature = 0.8 }
);
```

### 批量请求处理
```csharp
// 批量发送请求
var prompts = new List<string> 
{
    "生成殖民者姓名",
    "生成派系名称", 
    "生成事件描述"
};

var responses = await RimAIAPI.SendBatchRequestAsync(prompts);
foreach (var response in responses)
{
    Log.Message($"结果: {response}");
}
```

## 🎮 RimWorld集成示例

### 智能事件生成器
```csharp
public class AIEventGenerator 
{
    public async Task<GameEvent> GenerateRandomEvent()
    {
        var options = new LLMRequestOptions 
        {
            Temperature = 1.0, // 高创造性
            MaxTokens = 300
        };
        
        var prompt = @"为RimWorld生成一个随机事件，包含：
        - 事件标题
        - 事件描述
        - 可能的选择和后果";
        
        var response = await RimAIAPI.SendMessageAsync(prompt, options);
        
        // 解析响应并创建GameEvent对象
        return ParseEventFromResponse(response);
    }
}
```

### 智能殖民者对话系统
```csharp
public class ColonistDialogueSystem
{
    public async Task<string> GenerateDialogue(Pawn colonist, string situation)
    {
        var traits = string.Join(", ", colonist.story.traits.allTraits.Select(t => t.def.defName));
        
        var options = new LLMRequestOptions 
        {
            Temperature = 0.8,
            MaxTokens = 150
        };
        
        var prompt = $@"殖民者{colonist.Name}在以下情况下的反应：
        情况: {situation}
        性格特征: {traits}
        
        生成一句符合角色性格的对话：";
        
        return await RimAIAPI.SendMessageAsync(prompt, options);
    }
}
```

## ⚡ 性能优化特性

### 智能缓存策略
- 自动判断请求是否适合缓存
- LRU算法管理缓存条目
- 内存使用监控和自动清理

### 连接复用优化
- HTTP连接池管理
- DNS刷新机制
- 连接健康监控

### 批处理性能提升
- 并发请求处理
- 智能批量大小调整
- 错误隔离机制

## 🛠️ 开发者工具

### 实时监控命令
```csharp
// 获取框架统计信息
var stats = RimAIAPI.GetStatistics();
foreach (var stat in stats)
{
    Log.Message($"{stat.Key}: {stat.Value}");
}

// 清理缓存
RimAIAPI.ClearCache();

// 强制内存清理
FrameworkDiagnostics.ExecuteForceGarbageCollectionCommand();
```

### 健康检查和诊断
```csharp
// 定期健康检查
var healthResult = FrameworkDiagnostics.PerformHealthCheck();
if (!healthResult.IsHealthy)
{
    // 处理健康问题
    HandleHealthIssues(healthResult.Issues);
}

// 启用实时监控
var config = new FrameworkDiagnostics.MonitoringConfig
{
    EnableRealTimeMonitoring = true,
    MonitoringInterval = TimeSpan.FromMinutes(1),
    LogPerformanceAlerts = true
};
FrameworkDiagnostics.StartRealTimeMonitoring(config);
```

---

**RimAI Framework v3.0 - 为RimWorld模组开发提供最强大的AI集成能力！** 🚀

## 🔧 高级功能

### JSON响应处理
```csharp
// 使用预设的JSON选项
var options = RimAIAPI.Options.Structured();
var jsonResponse = await RimAIAPI.SendMessageAsync(
    "生成一个包含姓名、技能、背景的RimWorld角色信息",
    options
);

// 解析JSON响应
var characterData = JsonConvert.DeserializeObject<CharacterInfo>(jsonResponse);
```

### 流式响应与回调
```csharp
// 实时接收AI响应片段
await RimAIAPI.SendMessageStreamAsync(
    "描述一场RimWorld中的突袭战斗",
    chunk => {
        // 实时更新UI显示
        UpdateGameLogDisplay(chunk);
    },
    RimAIAPI.Options.Creative(0.9)
);
```

### 错误处理和重试机制
```csharp
// 内置重试机制的健壮请求
public async Task<string> RobustAIRequest(string prompt)
{
    var options = new LLMRequestOptions 
    {
        Temperature = 0.7,
        MaxTokens = 300,
        // 系统会自动处理重试
    };
    
    try 
    {
        return await RimAIAPI.SendMessageAsync(prompt, options);
    }
    catch (LLMException ex) when (ex.IsRecoverable)
    {
        // 对于可恢复的错误，框架已经自动重试
        Log.Warning($"AI请求经过重试后仍失败: {ex.Message}");
        return "抱歉，AI服务暂时不可用";
    }
}
```

## 🚀 性能优化特性

### 智能缓存策略
- **自动缓存判断**：系统智能识别可缓存的请求
- **LRU缓存管理**：自动清理最少使用的缓存条目
- **内存监控**：实时监控缓存内存使用情况
- **缓存统计**：提供详细的缓存命中率和性能指标

### 连接复用优化  
- **HTTP连接池**：复用TCP连接减少延迟
- **DNS缓存刷新**：定期更新DNS解析提高可靠性
- **连接健康监控**：自动检测和清理不健康的连接
- **超时管理**：智能超时控制避免资源浪费

### 批处理性能提升
- **并发控制**：使用信号量控制并发请求数量
- **错误隔离**：单个请求失败不影响整个批次
- **智能调度**：根据系统负载调整批处理策略
- **统计报告**：详细的批处理性能统计

### 内存优化
- **自动垃圾回收**：定期清理未使用的对象
- **对象池化**：重用请求和响应对象减少分配
- **流式处理**：大型响应的内存友好处理
- **资源清理**：及时释放网络和文件资源

## 📊 统计和监控

### 框架状态监控
```csharp
// 检查框架初始化状态
if (RimAIAPI.IsInitialized)
{
    Log.Message($"框架状态: {RimAIAPI.Status}");
    
    // 获取详细统计信息
    var stats = RimAIAPI.GetStatistics();
    foreach (var stat in stats)
    {
        Log.Message($"{stat.Key}: {stat.Value}");
    }
}
```

### 性能指标追踪
```csharp
// 生成性能报告
var report = FrameworkDiagnostics.GeneratePerformanceReport();

Log.Message($"API请求总数: {report.Metrics["API.TotalRequests"]}");
Log.Message($"缓存命中率: {report.Metrics["Cache.HitRate"]:P2}");
Log.Message($"内存使用: {report.Metrics["System.MemoryUsageMB"]:F1}MB");

// 查看系统建议
foreach (var recommendation in report.Recommendations)
{
    Log.Message($"性能建议: {recommendation}");
}
```

### 错误追踪和诊断
```csharp
// 健康状态检查
var healthCheck = FrameworkDiagnostics.PerformHealthCheck();

if (!healthCheck.IsHealthy)
{
    Log.Error($"系统状态: {healthCheck.Status}");
    
    // 记录所有问题
    foreach (var issue in healthCheck.Issues)
    {
        Log.Error($"严重问题: {issue}");
    }
    
    // 记录警告
    foreach (var warning in healthCheck.Warnings)
    {
        Log.Warning($"警告: {warning}");
    }
}
```

## 🔗 最佳实践示例

### 游戏事件生成器
```csharp
public class GameEventGenerator
{
    private readonly Dictionary<string, string> _eventCache = new();
    
    public async Task<string> GenerateRandomEvent(string eventType)
    {
        // 检查缓存
        if (_eventCache.TryGetValue(eventType, out var cachedEvent))
        {
            return cachedEvent;
        }
        
        var options = new LLMRequestOptions 
        {
            Temperature = 0.9,  // 高创造性
            MaxTokens = 250,
            EnableCaching = true
        };

        var prompt = $@"为RimWorld生成一个{eventType}类型的随机事件：
        - 事件名称
        - 简短描述  
        - 2-3个可能的玩家选择
        - 每个选择的潜在后果";

        try 
        {
            var result = await RimAIAPI.SendMessageAsync(prompt, options);
            _eventCache[eventType] = result; // 本地缓存
            return result;
        }
        catch (LLMException ex)
        {
            Log.Warning($"事件生成失败: {ex.Message}");
            return GetFallbackEvent(eventType);
        }
    }
    
    private string GetFallbackEvent(string eventType) => $"默认{eventType}事件";
}
```

### 智能NPC对话系统
```csharp
public class NPCDialogueSystem
{
    public async Task<string> GenerateDialogue(Pawn npc, string context, string playerInput)
    {
        // 构建角色上下文
        var traits = string.Join(", ", npc.story.traits.allTraits.Select(t => t.def.defName));
        var skills = GetTopSkills(npc, 3);
        
        var options = new LLMRequestOptions 
        {
            Temperature = 0.7,
            MaxTokens = 150,
            EnableCaching = true  // 相似对话可以复用
        };

        var prompt = $@"角色: {npc.Name?.ToStringShort ?? "未知"}
        性格特征: {traits}
        主要技能: {skills}
        当前情况: {context}
        玩家说: '{playerInput}'
        
        生成一句符合该角色性格的简短回应:";

        return await RimAIAPI.SendMessageAsync(prompt, options);
    }
    
    private string GetTopSkills(Pawn pawn, int count)
    {
        return string.Join(", ", 
            pawn.skills.skills
                .OrderByDescending(s => s.Level)
                .Take(count)
                .Select(s => $"{s.def.defName}({s.Level})"));
    }
}
```

### 错误恢复和缓存策略
```csharp
public class RobustAIService
{
    private readonly Dictionary<string, (string result, DateTime cached)> _cache = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
    
    public async Task<string> GetAIResponse(string prompt, LLMRequestOptions options = null)
    {
        var cacheKey = $"{prompt.GetHashCode():X8}_{options?.GetHashCode():X8}";
        
        // 检查本地缓存
        if (_cache.TryGetValue(cacheKey, out var cached) && 
            DateTime.UtcNow - cached.cached < _cacheExpiry)
        {
            return cached.result;
        }
        
        // 执行健康检查
        var health = FrameworkDiagnostics.PerformHealthCheck();
        if (!health.IsHealthy)
        {
            Log.Warning("系统不健康，使用降级处理");
            return GetFallbackResponse(prompt);
        }
        
        try 
        {
            options = options ?? new LLMRequestOptions { EnableCaching = true };
            var result = await RimAIAPI.SendMessageAsync(prompt, options);
            
            // 更新本地缓存
            _cache[cacheKey] = (result, DateTime.UtcNow);
            
            return result;
        }
        catch (Exception ex)
        {
            Log.Error($"AI请求失败: {ex.Message}");
            return GetFallbackResponse(prompt);
        }
    }
    
    private string GetFallbackResponse(string prompt) => "抱歉，AI服务暂时不可用";
}
```

## 🎮 开发者指南

### 预设选项使用
```csharp
// 使用内置预设快速配置
var creativeOptions = RimAIAPI.Options.Creative(0.9);    // 高创造性
var factualOptions = RimAIAPI.Options.Factual();         // 事实性回答  
var structuredOptions = RimAIAPI.Options.Structured();   // JSON格式输出

// 快速生成创意内容
var story = await RimAIAPI.SendMessageAsync("写个RimWorld短故事", creativeOptions);

// 获取准确信息
var info = await RimAIAPI.SendMessageAsync("RimWorld中如何提高种植技能", factualOptions);

// 生成结构化数据
var data = await RimAIAPI.SendMessageAsync("生成角色属性", structuredOptions);
```

### 自定义选项构建
```csharp
// 灵活构建自定义选项
var customOptions = new LLMRequestOptions 
{
    Temperature = 0.7,
    MaxTokens = 400,
    Model = "gpt-3.5-turbo",
    EnableCaching = true
};

// 或使用RimAIAPI.Options工厂方法的扩展
var advancedOptions = RimAIAPI.Options.Creative(0.8)
    .WithMaxTokens(500)
    .WithModel("gpt-4");
```

### 缓存管理策略
```csharp
// 手动清理缓存
RimAIAPI.ClearCache();

// 检查缓存状态
var cacheStats = ResponseCache.Instance.GetStats();
Log.Message($"缓存命中率: {cacheStats.HitRate:P2}");
Log.Message($"缓存条目数: {cacheStats.EntryCount}");
Log.Message($"内存使用: {cacheStats.MemoryUsageEstimate / (1024 * 1024):F1}MB");

// 当内存使用过高时自动清理
if (cacheStats.MemoryUsageEstimate > 100 * 1024 * 1024) // 100MB
{
    ResponseCache.Instance.Clear();
    Log.Message("已清理缓存以释放内存");
}
```

### 监控和调试
```csharp
// 启用详细日志记录
public class AIDebugger 
{
    public static async Task<string> DebugAIRequest(string prompt) 
    {
        Log.Message($"[AI Debug] 发送请求: {prompt}");
        
        var startTime = DateTime.UtcNow;
        var options = new LLMRequestOptions { EnableCaching = false }; // 禁用缓存用于调试
        
        try 
        {
            var response = await RimAIAPI.SendMessageAsync(prompt, options);
            var duration = DateTime.UtcNow - startTime;
            
            Log.Message($"[AI Debug] 请求成功，耗时: {duration.TotalMilliseconds:F0}ms");
            Log.Message($"[AI Debug] 响应长度: {response.Length} 字符");
            
            return response;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Log.Error($"[AI Debug] 请求失败，耗时: {duration.TotalMilliseconds:F0}ms，错误: {ex.Message}");
            throw;
        }
    }
}
```

---

**RimAI Framework v3.0 - 为RimWorld模组开发提供最强大的AI集成能力！** 🚀
