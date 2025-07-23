# 🚀 RimAI Framework v3.0 快速上手指南

## ⚡ 5分钟快速开始

### 第一步：添加引用
```csharp
using RimAI.Framework.API;
using RimAI.Framework.LLM.Models;
```

### 第二步：发送第一个请求
```csharp
// 最简单的调用
var response = await RimAIAPI.SendMessageAsync("Hello, AI!");
Log.Message(response.Content);
```

### 第三步：处理响应
```csharp
if (response.IsSuccess)
{
    Log.Message($"AI回复: {response.Content}");
}
else
{
    Log.Error($"请求失败: {response.ErrorMessage}");
}
```

🎉 **恭喜！您已经成功使用RimAI Framework！**

---

## 📋 常用场景速查

### 🎲 创意内容生成
```csharp
// 使用创意模式生成故事
var story = await RimAIAPI.SendMessageAsync(
    "写一个关于RimWorld殖民者的短故事",
    RimAIAPI.Options.Creative()
);
```

### 📊 数据分析查询
```csharp
// 使用事实模式进行数据分析
var analysis = await RimAIAPI.SendMessageAsync(
    "分析当前殖民地的资源状况",
    RimAIAPI.Options.Factual()
);
```

### 🔄 实时流式响应
```csharp
// 流式接收长文本回复
await RimAIAPI.SendMessageStreamAsync(
    "详细解释RimWorld的游戏机制",
    chunk => Log.Message($"接收: {chunk}")
);
```

### 📦 批量处理
```csharp
// 批量翻译文本
var texts = new List<string> { "Hello", "World", "RimWorld" };
var translations = await RimAIAPI.SendBatchRequestAsync(
    texts.Select(t => $"翻译成中文: {t}").ToList()
);
```

### 🤖 函数调用 (Function Calling)
```csharp
// 让AI决定是否以及如何调用你提供的工具（函数）
var tools = new List<AITool> { /* ... 定义你的工具 ... */ };
var prompt = "128乘以5.5等于多少？";

// AI会返回它认为应该调用的函数名和参数
var functionCallResults = await RimAIAPI.GetFunctionCallAsync(prompt, tools);

if (functionCallResults != null)
{
    foreach(var call in functionCallResults)
    {
        Log.Message($"函数名: {call.FunctionName}");
        Log.Message($"参数 (JSON): {call.Arguments}");
        // 接下来，你需要自己执行这个函数
    }
}
```

---

## 🎛️ 预设选项快速使用

| 场景 | 预设选项 | 适用情况 |
|------|----------|----------|
| 创意写作 | `RimAIAPI.Options.Creative()` | 故事创作、想象内容 |
| 事实问答 | `RimAIAPI.Options.Factual()` | 数据查询、分析报告 |
| 结构化输出 | `RimAIAPI.Options.Structured()` | JSON格式、数据导出 |
| 流式响应 | `RimAIAPI.Options.Streaming()` | 长文本、实时对话 |

### 使用示例

```csharp
// ✅ 创意模式 - 温度1.0，适合创作
var poem = await RimAIAPI.SendMessageAsync(
    "写一首关于太空的诗", 
    RimAIAPI.Options.Creative()
);

// ✅ 事实模式 - 温度0.2，适合查询
var info = await RimAIAPI.SendMessageAsync(
    "RimWorld最新版本特性", 
    RimAIAPI.Options.Factual()
);

// ✅ 结构化模式 - 适合JSON输出
var data = await RimAIAPI.SendMessageAsync(
    "以JSON格式返回殖民地统计", 
    RimAIAPI.Options.Structured()
);
```

---

## 🔧 自定义配置

### 基础配置
```csharp
var options = new LLMRequestOptions
{
    Temperature = 0.7f,        // 创造性 (0.0-2.0)
    MaxTokens = 500,           // 最大长度
    EnableCaching = true,      // 启用缓存
    TimeoutSeconds = 30        // 超时时间
};

var response = await RimAIAPI.SendMessageAsync("你的消息", options);
```

### 高级配置
```csharp
var advancedOptions = new LLMRequestOptions
{
    Temperature = 0.8f,
    MaxTokens = 1000,
    TopP = 0.9f,                                    // Top-p采样
    FrequencyPenalty = 0.1f,                        // 频率惩罚
    PresencePenalty = 0.1f,                         // 存在惩罚
    StopWords = new List<string> { "结束", "完成" },   // 停止词
    RetryCount = 3,                                 // 重试次数
    UserId = "player1"                              // 用户标识
};
```

---

## 📊 监控和统计

### 查看框架状态
```csharp
var stats = RimAIAPI.GetStatistics();

Log.Message($"总请求数: {stats.TotalRequests}");
Log.Message($"成功率: {stats.SuccessfulRequests * 100.0 / stats.TotalRequests:F1}%");
Log.Message($"平均响应时间: {stats.AverageResponseTime:F0}ms");
Log.Message($"缓存命中率: {stats.CacheHitRate:P2}");
```

### 性能优化检查
```csharp
// 检查性能指标
if (stats.AverageResponseTime > 5000)
    Log.Warning("响应时间过长，检查网络连接");

if (stats.CacheHitRate < 0.2)
    Log.Warning("缓存命中率低，考虑优化请求");

// 清理缓存
if (stats.TotalRequests > 1000)
{
    RimAIAPI.ClearCache();
    Log.Message("缓存已清理");
}
```

---

## ⚠️ 错误处理

### 基础错误处理
```csharp
try
{
    var response = await RimAIAPI.SendMessageAsync("你好");
    if (response.IsSuccess)
    {
        // 处理成功响应
        Log.Message(response.Content);
    }
    else
    {
        // 处理失败响应
        Log.Error($"请求失败: {response.ErrorMessage}");
    }
}
catch (Exception ex)
{
    Log.Error($"异常: {ex.Message}");
}
```

### 特定异常处理
```csharp
try
{
    var response = await RimAIAPI.SendMessageAsync(longMessage);
}
catch (TokenLimitException)
{
    Log.Warning("消息过长，尝试缩短内容");
}
catch (ConnectionException)
{
    Log.Warning("网络连接问题，请检查网络");
}
catch (ConfigurationException)
{
    Log.Error("配置错误，请检查API设置");
}
```

---

## 🎯 实际应用案例

### 案例1：智能NPC对话
```csharp
public async Task<string> GenerateNPCDialogue(string playerInput, string npcPersonality)
{
    var prompt = $"角色设定：{npcPersonality}\n玩家说：{playerInput}\n请回复：";
    
    var response = await RimAIAPI.SendMessageAsync(
        prompt,
        RimAIAPI.Options.Creative()
    );
    
    return response.IsSuccess ? response.Content : "...";
}

// 使用
var dialogue = await GenerateNPCDialogue(
    "你好，有什么任务吗？",
    "一个友善的商人，喜欢谈论贸易"
);
```

### 案例2：事件描述生成
```csharp
public async Task<string> GenerateEventDescription(string eventType)
{
    var response = await RimAIAPI.SendMessageAsync(
        $"为RimWorld生成一个{eventType}事件的详细描述",
        new LLMRequestOptions 
        { 
            Temperature = 0.9f,
            MaxTokens = 300 
        }
    );
    
    return response.Content;
}
```

### 案例3：殖民地状态分析
```csharp
public async Task AnalyzeColonyStatus()
{
    var colonyData = GatherColonyData(); // 收集殖民地数据
    
    var analysis = await RimAIAPI.SendMessageAsync(
        $"分析以下殖民地数据并给出建议：\n{colonyData}",
        RimAIAPI.Options.Factual()
    );
    
    if (analysis.IsSuccess)
    {
        ShowAnalysisDialog(analysis.Content);
    }
}
```

### 案例4：批量物品描述
```csharp
public async Task GenerateItemDescriptions(List<ThingDef> items)
{
    var prompts = items.Select(item => 
        $"为物品'{item.label}'生成有趣的描述").ToList();
    
    var descriptions = await RimAIAPI.SendBatchRequestAsync(
        prompts,
        new LLMRequestOptions { MaxTokens = 100 }
    );
    
    for (int i = 0; i < items.Count; i++)
    {
        if (descriptions[i].IsSuccess)
        {
            items[i].description = descriptions[i].Content;
        }
    }
}
```

---

## 🔄 流式响应高级用法

### 实时对话系统
```csharp
public async Task StartRealTimeChat(string initialMessage)
{
    var conversationText = new StringBuilder();
    
    await RimAIAPI.SendMessageStreamAsync(
        initialMessage,
        chunk => {
            conversationText.Append(chunk);
            
            // 实时更新UI
            UpdateChatUI(conversationText.ToString());
            
            // 检查是否包含特定关键词
            if (chunk.Contains("任务完成"))
            {
                CompleteCurrentTask();
            }
        },
        RimAIAPI.Options.Streaming()
    );
}
```

### 带进度的长文本生成
```csharp
public async Task GenerateLongStory(string theme)
{
    var storyParts = new List<string>();
    var currentPart = new StringBuilder();
    
    await RimAIAPI.SendMessageStreamAsync(
        $"写一个关于{theme}的长篇故事",
        chunk => {
            currentPart.Append(chunk);
            
            // 检查是否到了段落结尾
            if (chunk.Contains("\n\n"))
            {
                storyParts.Add(currentPart.ToString());
                currentPart.Clear();
                
                // 更新进度
                UpdateProgress(storyParts.Count);
            }
        },
        new LLMRequestOptions 
        { 
            MaxTokens = 2000,
            Temperature = 1.1f 
        }
    );
}
```

---

## 💡 性能优化技巧

### 1. 启用缓存
```csharp
// ✅ 对相似请求启用缓存
var cachedOptions = new LLMRequestOptions { EnableCaching = true };

// ❌ 避免对每次都不同的请求启用缓存
var noCacheOptions = new LLMRequestOptions { EnableCaching = false };
```

#### 🔍 系统如何判断"相似请求"？

RimAI Framework使用**智能缓存键生成算法**来判断请求相似性：

**缓存键构成要素**：
```csharp
// 缓存键格式：LLM:{消息哈希}:temp={温度}:maxtok={最大Token}:model={模型}:json={JSON模式}...
// 示例键：LLM:12345678:temp=0.7:maxtok=500:model=gpt-3.5-turbo:json=False
```

**判断相似的核心逻辑**：
1. **消息内容**：使用 `GetHashCode()` 生成消息的哈希值
2. **关键参数**：Temperature、MaxTokens、Model、JsonMode等
3. **可选参数**：TopP、JsonSchema、FrequencyPenalty等

**相似请求示例**：
```csharp
// ✅ 这两个请求会被判断为相同，使用缓存
var request1 = await RimAIAPI.SendMessageAsync("Hello World", 
    new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 100 });
    
var request2 = await RimAIAPI.SendMessageAsync("Hello World", 
    new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 100 });

// ❌ 这两个请求不同，不会使用缓存
var request3 = await RimAIAPI.SendMessageAsync("Hello World", 
    new LLMRequestOptions { Temperature = 0.8f, MaxTokens = 100 }); // 温度不同
```

**优化缓存命中的技巧**：
```csharp
// ✅ 标准化参数提高缓存命中
var standardOptions = new LLMRequestOptions 
{
    Temperature = 0.7f,     // 使用固定的常用值
    MaxTokens = 500,        // 使用标准长度
    EnableCaching = true
};

// ✅ 对于事实性问题，使用更低的温度
var factualOptions = new LLMRequestOptions 
{
    Temperature = 0.2f,     // 低温度，更容易缓存命中
    EnableCaching = true
};
```

### 2. 合理设置温度
```csharp
// ✅ 事实性内容使用低温度（提高缓存命中）
var factOptions = new LLMRequestOptions { Temperature = 0.1f };

// ✅ 创意内容使用高温度
var creativeOptions = new LLMRequestOptions { Temperature = 1.0f };
```

### 3. 批量处理优化
```csharp
// ✅ 批量处理多个相似请求
var batchRequests = PrepareMultipleRequests();
var batchResults = await RimAIAPI.SendBatchRequestAsync(batchRequests);

// ❌ 避免在循环中单独发送多个请求
foreach (var request in requests)
{
    // 这样做效率低下
    var result = await RimAIAPI.SendMessageAsync(request);
}
```

### 4. 超时和重试设置
```csharp
// ✅ 合理设置超时和重试
var robustOptions = new LLMRequestOptions 
{
    TimeoutSeconds = 30,    // 30秒超时
    RetryCount = 3          // 重试3次
};
```

---

## 🚨 常见问题解决

### Q: 请求太慢怎么办？
```csharp
// A: 减少MaxTokens，启用缓存，使用批量处理
var fastOptions = new LLMRequestOptions 
{
    MaxTokens = 200,        // 减少长度
    EnableCaching = true,   // 启用缓存
    Temperature = 0.3f      // 低温度提高缓存命中
};
```

### Q: 消息太长被截断？
```csharp
// A: 分割长消息或增加MaxTokens
if (message.Length > 2000)
{
    var chunks = SplitMessage(message, 1500);
    var responses = await RimAIAPI.SendBatchRequestAsync(chunks);
    var fullResponse = string.Join("", responses.Select(r => r.Content));
}
```

### Q: 网络不稳定导致失败？
```csharp
// A: 增加重试次数和超时时间
var stableOptions = new LLMRequestOptions 
{
    TimeoutSeconds = 60,
    RetryCount = 5
};
```

### Q: 内存占用过高？
```csharp
// A: 定期清理缓存
var stats = RimAIAPI.GetStatistics();
if (stats.TotalRequests > 500)
{
    RimAIAPI.ClearCache();
    GC.Collect(); // 强制垃圾回收
}
```

---

## 📚 进阶学习资源

### 详细文档
- [API详细调用指南](CN_v3.0_API详细调用指南.md) - 完整API参考
- [功能特性](CN_v3.0_功能特性.md) - 详细功能介绍
- [架构设计](CN_v3.0_架构改造完成报告.md) - 技术架构

### 升级指导
- [迁移指南](CN_v3.0_迁移指南.md) - 从v2.x升级

---

## ⭐ 快速参考卡片

### 基础调用
```csharp
// 简单请求
var response = await RimAIAPI.SendMessageAsync("消息");

// 带配置请求
var response = await RimAIAPI.SendMessageAsync("消息", options);

// 流式请求
await RimAIAPI.SendMessageStreamAsync("消息", chunk => { });

// 批量请求
var responses = await RimAIAPI.SendBatchRequestAsync(messages);
```

### 预设选项
```csharp
RimAIAPI.Options.Creative()    // 创意模式
RimAIAPI.Options.Factual()     // 事实模式  
RimAIAPI.Options.Structured()  // 结构化输出
RimAIAPI.Options.Streaming()   // 流式优化
```

### 监控统计
```csharp
var stats = RimAIAPI.GetStatistics();  // 获取统计
RimAIAPI.ClearCache();                 // 清理缓存
```

---

🎉 **恭喜！您现在已经掌握了RimAI Framework v3.0的基本使用方法！**

**开始您的AI增强RimWorld之旅吧！** 🚀
