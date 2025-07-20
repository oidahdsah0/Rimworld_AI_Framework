# RimAI Framework v3.0 升级迁移指南

## 🌟 升级概述

RimAI Framework v3.0 引入了全新的统一架构，提供更强大的功能和更好的性能。本指南将帮助您从v2.x平滑升级到v3.0。

### 🎯 主要收益
- ✅ **统一架构**：单一API入口，简化使用方式
- ✅ **强化缓存**：智能LRU缓存，显著提升性能
- ✅ **健壮异常处理**：结构化异常系统，更好的错误恢复
- ✅ **全面监控**：健康检查、性能监控、调试工具
- ✅ **生命周期管理**：自动资源管理，内存泄漏防护
- ✅ **向后兼容**：主要API保持兼容，升级无痛

## ⚠️ 重大变更

### 1. 架构变更
- 🚫 **移除服务层**：不再使用 `ILLMService` 接口
- 🚫 **统一执行器**：单一的 `LLMExecutor` 处理所有请求
- 🚫 **移除Legacy类**：`Message`、旧版`LLMRequest` 等已移除
- ✅ **新增组件**：生命周期管理器、缓存系统、配置管理器

### 2. API变更
- 🔄 **命名空间**：所有API统一在 `RimAIAPI` 静态类中
- 🔄 **选项系统**：使用 `LLMRequestOptions` 代替匿名对象
- 🔄 **预设选项**：新增 `RimAIAPI.Options.*` 工厂方法

## 📋 详细迁移步骤

### Step 1: 更新引用

**无需改动**：RimAI Framework v3.0 保持相同的程序集名称和主要命名空间。

```csharp
// 引用保持不变
using RimAI.Framework.API;
using RimAI.Framework.LLM.Models;
```

### Step 2: 基础API迁移

#### 2.1 基本消息发送（向后兼容）
```csharp
// v2.x 代码（仍然有效）
var response = await RimAIAPI.SendMessageAsync("Hello World");

// v3.0 推荐写法（性能更好）
var options = new LLMRequestOptions { Temperature = 0.7 };
var response = await RimAIAPI.SendMessageAsync("Hello World", options);
```

#### 2.2 参数设置修改
```csharp
// ❌ v2.x 方式（可能无法正常工作）
var response = await RimAIAPI.SendMessageAsync("Hello", new { temperature = 0.8, max_tokens = 500 });

// ✅ v3.0 正确方式
var options = new LLMRequestOptions 
{
    Temperature = 0.8,
    MaxTokens = 500
};
var response = await RimAIAPI.SendMessageAsync("Hello", options);

// ✅ v3.0 流畅API方式
var options = RimAIAPI.Options.Creative(0.8).WithMaxTokens(500);
var response = await RimAIAPI.SendMessageAsync("Hello", options);
```

### Step 3: 流式请求迁移

```csharp
// ❌ v2.x 可能的方式（如果存在）
// var response = await RimAIAPI.SendStreamingMessageAsync("Hello", chunk => Log.Message(chunk));

// ✅ v3.0 正确方式
await RimAIAPI.SendMessageStreamAsync(
    "Hello", 
    chunk => Log.Message(chunk),
    RimAIAPI.Options.Creative()
);
```

### Step 4: 错误处理更新

```csharp
// v2.x 基础错误处理
try 
{
    var response = await RimAIAPI.SendMessageAsync("Hello");
}
catch (Exception ex)
{
    Log.Error($"AI请求失败: {ex.Message}");
}

// ✅ v3.0 增强错误处理
try 
{
    var response = await RimAIAPI.SendMessageAsync("Hello");
}
catch (LLMException ex)
{
    Log.Error($"LLM服务错误 [{ex.ErrorCode}]: {ex.Message}");
    
    if (ex.IsRecoverable)
    {
        // 框架已自动重试，但仍失败
        Log.Warning("将使用降级策略");
    }
}
catch (ConnectionException ex)
{
    Log.Error($"网络连接错误: {ex.Message}, 已重试 {ex.RetryCount} 次");
}
catch (ConfigurationException ex)
{
    Log.Error($"配置错误: {ex.Message} (文件: {ex.ConfigurationFile})");
}
```

## 🔄 功能迁移对照

### 温度参数设置
```csharp
// ❌ v2.x 问题代码（参数可能被忽略）
await RimAIAPI.SendMessageAsync("Hello", new { temperature = 0.9 });

// ✅ v3.0 解决方案1：使用选项对象
var options = new LLMRequestOptions { Temperature = 0.9 };
await RimAIAPI.SendMessageAsync("Hello", options);

// ✅ v3.0 解决方案2：使用预设
await RimAIAPI.SendCreativeMessageAsync("Hello", 0.9);

// ✅ v3.0 解决方案3：使用工厂方法
var options = RimAIAPI.Options.Creative(0.9);
await RimAIAPI.SendMessageAsync("Hello", options);
```

### 批量请求处理
```csharp
// v2.x 可能的实现
var responses = new List<string>();
foreach (var prompt in prompts)
{
    responses.Add(await RimAIAPI.SendMessageAsync(prompt));
}

## 💡 新功能使用指南

### 1. 使用新的缓存系统
```csharp
// v3.0 自动缓存（推荐）
var options = new LLMRequestOptions { EnableCaching = true }; // 默认已启用
var response = await RimAIAPI.SendMessageAsync("Hello", options);

// 检查缓存状态
var stats = ResponseCache.Instance.GetStats();
Log.Message($"缓存命中率: {stats.HitRate:P2}");

// 手动清理缓存
RimAIAPI.ClearCache();
```

### 2. 健康监控和诊断
```csharp
// v3.0 新增：系统健康检查
var health = FrameworkDiagnostics.PerformHealthCheck();
if (!health.IsHealthy)
{
    Log.Warning($"系统状态异常: {health.Status}");
}

// 性能监控
var report = FrameworkDiagnostics.GeneratePerformanceReport();
Log.Message(report.Summary);
```

### 3. 配置管理
```csharp
// v3.0 新增：集中配置管理
var config = RimAIConfiguration.Instance;
var timeout = config.Get<int>("HTTP.TimeoutSeconds", 30);
var maxCacheSize = config.Get<int>("Cache.MaxSize", 100);
```

### 4. 生命周期管理
```csharp
// v3.0 自动管理（无需手动调用）
// 系统会自动：
// - 定期健康检查
// - 内存监控和垃圾回收  
// - 优雅关闭时清理资源
// - 连接池管理

// 可选：获取状态信息
var lifecycle = LifecycleManager.Instance;
Log.Message($"框架健康状态: {lifecycle.IsHealthy}");
```

## 🚨 常见迁移问题

### 问题1：Temperature参数不生效

**问题现象**：
```csharp
// 此代码在v2.x中可能不工作
var response = await RimAIAPI.SendMessageAsync("Hello", new { temperature = 0.9 });
```

**解决方案**：
```csharp
// ✅ 使用LLMRequestOptions
var options = new LLMRequestOptions { Temperature = 0.9 };
var response = await RimAIAPI.SendMessageAsync("Hello", options);

// ✅ 或使用预设选项
var response = await RimAIAPI.SendCreativeMessageAsync("Hello", 0.9);
```

### 问题2：找不到旧的API方法

**问题现象**：
```csharp
// 如果v2.x中有这样的方法（现在找不到）
// await RimAI.SomeOldMethod();
```

**解决方案**：
```csharp
// ✅ 所有功能都统一到RimAIAPI中
await RimAIAPI.SendMessageAsync("your prompt");

// 查看可用方法：
// - SendMessageAsync()
// - SendMessageStreamAsync()  
// - SendBatchRequestAsync()
// - SendCreativeMessageAsync()
// - GetStatistics()
// - ClearCache()
```

### 问题3：异常类型变化

**v2.x可能的异常处理**：
```csharp
catch (Exception ex)
{
    // 泛泛处理所有异常
}
```

**v3.0改进的异常处理**：
```csharp
catch (LLMException ex) when (ex.IsRecoverable)
{
    // LLM服务异常，但可以重试
    Log.Warning($"LLM服务暂时不可用: {ex.Message}");
}
catch (LLMException ex)
{
    // LLM服务异常，不可恢复
    Log.Error($"LLM服务错误: {ex.Message}");
}
catch (ConnectionException ex)
{
    // 网络连接问题
    Log.Error($"网络连接失败: {ex.Message}");
}
```

## ✅ 迁移验证清单

### 基础功能测试
- [ ] 基本消息发送正常工作
- [ ] 温度参数正确生效
- [ ] 错误处理捕获正确的异常类型
- [ ] 缓存系统正常运作

### 高级功能测试  
- [ ] 流式响应正常接收
- [ ] 批量请求正确处理
- [ ] 性能监控数据正常
- [ ] 配置系统可正常访问

### 性能验证
- [ ] 响应时间没有明显下降
- [ ] 内存使用保持稳定
- [ ] 缓存命中率符合预期
- [ ] 错误恢复机制正常

## 🔄 回滚计划

如果升级后遇到严重问题，可以：

1. **保留v2.x备份**：升级前备份整个mod文件夹
2. **配置回滚**：RimAI会自动兼容v2.x的配置文件
3. **数据迁移**：v3.0不会破坏现有的游戏存档

---

**升级到RimAI Framework v3.0，享受更强大、更稳定的AI功能！** 🚀
| `WithSeed(int)` | 设置随机种子 | `.WithSeed(42)` |
| `WithCustomParameter(string, object)` | 自定义参数 | `.WithCustomParameter("logprobs", true)` |

### 便捷方法

| 方法 | 描述 |
|------|------|
| `SendCreativeMessageAsync()` | 高温度创意请求 |
| `SendFactualMessageAsync()` | 低温度事实请求 |
| `SendJsonRequestAsync()` | JSON格式请求 |
| `SendMessageWithSystemAsync()` | 带系统提示的请求 |
| `SendCustomRequestAsync()` | 带自定义参数的请求 |

### 预设选项

```csharp
// 创意模式
var creative = LLMRequestOptions.Creative(1.2);

// 事实模式
var factual = LLMRequestOptions.Factual(0.3);

// JSON模式
var json = LLMRequestOptions.Json(schema, 0.7);

// 流式模式
var streaming = LLMRequestOptions.Streaming(0.8, 1000);
```

## ⚠️ 重要注意事项

### 1. 向后兼容性
- 基本的 `SendMessageAsync()` 方法保持兼容
- 大多数现有代码无需修改即可继续工作
- 但推荐升级到新的参数化方法以获得完整功能

### 2. Temperature 覆盖
- v3.0 确保 Temperature 参数正确覆盖全局设置
- 使用 `LLMRequestOptions.WithTemperature()` 或便捷方法
- 旧的对象参数方式已废弃

### 3. 性能改进
- 统一架构减少了内存分配
- 直接执行路径提高了响应速度
- 支持更高的并发请求

## 🚀 推荐的最佳实践

### 1. 优先使用新API
```csharp
// 推荐：使用参数化API
var options = new LLMRequestOptions().WithTemperature(0.8);
var response = await RimAIAPI.SendMessageAsync(prompt, options);

// 而不是：基本API
var response = await RimAIAPI.SendMessageAsync(prompt);
```

### 2. 充分利用流畅API
```csharp
var options = new LLMRequestOptions()
    .WithTemperature(0.7)
    .WithMaxTokens(500)
    .WithJsonOutput()
    .WithStopSequences("END");
```

### 3. 使用专用方法
```csharp
// JSON请求使用专用方法
var json = await RimAIAPI.SendJsonRequestAsync(prompt);

// 创意请求使用便捷方法
var creative = await RimAIAPI.SendCreativeMessageAsync(prompt, 1.2);
```

### 4. 合理使用自定义参数
```csharp
var options = new LLMRequestOptions()
    .WithCustomParameter("presence_penalty", 0.6)
    .WithCustomParameter("frequency_penalty", 0.3)
    .WithSeed(42);
```

## 📝 示例代码集合

完整的示例请参考：
- `EnhancedArchitectureExamples.cs` - 包含所有新功能的示例
- `UnifiedArchitectureExamples.cs` - 统一架构使用示例

## 🐛 故障排除

### 常见问题

**Q: Temperature 参数没有生效？**
A: 确保使用 `LLMRequestOptions.WithTemperature()` 而不是直接传递对象参数。

**Q: JSON输出格式不正确？**
A: 使用 `SendJsonRequestAsync()` 或 `WithJsonOutput()` 方法。

**Q: 自定义参数不工作？**
A: 检查参数名称是否正确，使用 `WithCustomParameter()` 方法。

**Q: 编译错误？**
A: 确保添加了 `using RimAI.Framework.LLM;` 引用。

---

**升级完成！享受新的统一架构带来的强大功能！** 🎉
