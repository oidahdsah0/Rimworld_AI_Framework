n# 📘 RimAI Framework v3.0 API详细调用指南

## 📋 概述

本文档提供RimAI Framework v3.0所有API的详细调用方法、参数说明和高级用法。适合需要深入了解框架功能的开发者。

## 🎯 核心API类：RimAIAPI

`RimAIAPI` 是框架的统一入口点，提供所有主要功能的静态方法。

### 命名空间
```csharp
using RimAI.Framework.API;
using RimAI.Framework.LLM.Models;
```

## 1. 基础API vs 高级API

为了满足不同开发者的需求，RimAI v3.0 提供了两种核心API：

- **基础API (`SendMessageAsync`)**: 
  - **返回**: `string`
  - **特点**: 简单、直接，快速获取文本结果。
  - **适用场景**: 适用于不关心错误细节、Token消耗等元数据的简单请求。
  
- **高级API (`SendRequestAsync`)**: 
  - **返回**: `LLMResponse` 对象
  - **特点**: 功能强大，提供详细的成功/失败状态、错误信息和元数据。
  - **适用场景**: 推荐用于需要可靠错误处理、访问响应元数据（如Token使用情况）的生产级功能。

---

## 2. SendMessageAsync - 基础消息发送 (返回 string)

此方法是与AI进行简单交互的最快方式。它直接返回AI的响应文本，如果发生错误则返回`null`。

#### 方法签名
```csharp
public static async Task<string> SendMessageAsync(
    string message, 
    LLMRequestOptions options = null
)
```

#### 参数详解

**message (string, 必需)**
- 发送给LLM的消息内容
- 支持多行文本和特殊字符
- 建议长度：1-8000字符

**options (LLMRequestOptions, 可选)**
- 请求配置选项，null时使用默认配置
- 详细参数见 [LLMRequestOptions](#llmrequestoptions-详细参数)

#### 返回值：string
- 返回AI生成的响应内容字符串
- 如果请求失败，返回null
- 框架内部已处理错误和重试机制

#### 使用示例

**基础调用**
```csharp
// 最简单的调用方式
var response = await RimAIAPI.SendMessageAsync("Hello, AI!");
if (!string.IsNullOrEmpty(response))
{
    Log.Message($"AI回复: {response}");
}
```

**带参数调用**
```csharp
// 使用自定义配置
var options = new LLMRequestOptions
{
    Temperature = 0.8f,
    MaxTokens = 500,
    EnableCaching = true
};

var response = await RimAIAPI.SendMessageAsync(
    "写一个关于RimWorld的短故事", 
    options
);
```

**错误处理**
```csharp
try
{
    var response = await RimAIAPI.SendMessageAsync("你好");
    if (string.IsNullOrEmpty(response))
    {
        Log.Error("请求失败：未收到响应");
        return;
    }
    
    // 处理成功响应
    ProcessResponse(response);
}
catch (RimAIException ex)
{
    Log.Error($"RimAI异常: {ex.Message}");
}
catch (Exception ex)
{
    Log.Error($"未知错误: {ex.Message}");
}
```

### 2. SendMessageStreamAsync - 流式消息发送

#### 方法签名
```csharp
public static async Task SendMessageStreamAsync(
    string message,
    Action<string> onChunkReceived,
    LLMRequestOptions options = null,
    CancellationToken cancellationToken = default
)
```

#### 参数详解

**message (string, 必需)**
- 发送给LLM的消息内容

**onChunkReceived (Action<string>, 必需)**
- 接收响应块的回调函数
- 每次收到新的响应片段时触发
- 参数是响应内容的片段

**options (LLMRequestOptions, 可选)**
- 请求配置选项

**cancellationToken (CancellationToken, 可选)**
- 用于取消长时间运行的请求

#### 使用示例

**基础流式调用**
```csharp
var fullResponse = new StringBuilder();

await RimAIAPI.SendMessageStreamAsync(
    "详细解释RimWorld的战斗系统",
    chunk => {
        // 实时接收响应片段
        Log.Message($"收到: {chunk}");
        fullResponse.Append(chunk);
    }
);

Log.Message($"完整响应: {fullResponse.ToString()}");
```

**带取消功能的流式调用**
```csharp
var cts = new CancellationTokenSource();
var responseBuilder = new StringBuilder();

// 设置5秒超时
cts.CancelAfter(TimeSpan.FromSeconds(5));

try
{
    await RimAIAPI.SendMessageStreamAsync(
        "写一篇长篇小说",
        chunk => {
            responseBuilder.Append(chunk);
            
            // 可以在回调中检查条件并取消
            if (responseBuilder.Length > 1000)
            {
                cts.Cancel();
            }
        },
        options: new LLMRequestOptions { Temperature = 0.9f },
        cancellationToken: cts.Token
    );
}
catch (OperationCanceledException)
{
    Log.Message("请求被取消");
}
```

**实时UI更新示例**
```csharp
var dialog = Find.WindowStack.WindowOfType<MyAIDialog>();

await RimAIAPI.SendMessageStreamAsync(
    userInput,
    chunk => {
        // 在主线程更新UI
        if (Current.ProgramState == ProgramState.Playing)
        {
            dialog?.UpdateResponseText(chunk);
        }
    },
    new LLMRequestOptions { EnableCaching = false }
);
```

### 3. SendRequestAsync - 高级消息发送 (返回 LLMResponse)

此方法提供了更强大的功能和更精细的控制，返回一个包含所有响应细节的`LLMResponse`对象。**这是推荐用于大多数生产级功能的方法。**

#### 方法签名
```csharp
public static async Task<LLMResponse> SendRequestAsync(
    string prompt,
    LLMRequestOptions options = null,
    CancellationToken cancellationToken = default
)
```

#### 返回值：LLMResponse

`LLMResponse` 对象包含了所有关于请求响应的详细信息：
```csharp
public class LLMResponse
{
    // 核心内容
    public string Content { get; }           // 提取出的主要响应文本
    public List<ToolCall> ToolCalls { get; } // AI请求的工具调用列表

    // 状态与错误处理
    public bool IsSuccess { get; }           // 请求是否成功
    public string ErrorMessage { get; }     // 失败时的详细错误信息

    // 元数据
    public string Id { get; set; }              // 响应的唯一ID
    public string Model { get; set; }           // 使用的模型名称
    public Usage Usage { get; set; }           // Token使用情况统计
    public string RequestId { get; }         // 内部请求ID
}

public class Usage
{
    public int PromptTokens { get; set; }     // 输入的Token数
    public int CompletionTokens { get; set; } // 输出的Token数
    public int TotalTokens { get; set; }      // 总Token数
}
```

#### 使用示例

**可靠的请求与错误处理**
```csharp
var response = await RimAIAPI.SendRequestAsync("生成一个关于太空商船的背景故事");

if (response.IsSuccess)
{
    Log.Message($"成功！AI回复: {response.Content}");
    
    // 你还可以检查Token使用情况
    if (response.Usage != null)
    {
        Log.Message($"本次请求消耗了 {response.Usage.TotalTokens} 个Token。");
    }
}
else
{
    // 精确地知道错误原因
    Log.Error($"AI请求失败: {response.ErrorMessage}");
}
```

---

### 4. SendBatchRequestAsync - 批量请求处理

#### 方法签名
```csharp
public static async Task<List<string>> SendBatchRequestAsync(
    List<string> messages,
    LLMRequestOptions options = null
)
```

#### 参数详解

**messages (List<string>, 必需)**
- 要批量处理的消息列表
- 建议批量大小：1-10个消息
- 框架会自动优化并发处理

**options (LLMRequestOptions, 可选)**
- 应用于所有请求的配置选项

#### 返回值：List<string>
- 返回响应内容字符串列表，顺序与输入消息对应
- 失败的请求在列表中对应位置为null
- 即使某个请求失败，其他请求仍会继续处理

#### 使用示例

**批量翻译**
```csharp
var texts = new List<string>
{
    "Hello World",
    "Good Morning", 
    "How are you?",
    "Thank you"
};

var responses = await RimAIAPI.SendBatchRequestAsync(
    texts.Select(t => $"将以下英文翻译成中文：{t}").ToList(),
    new LLMRequestOptions { Temperature = 0.3f }
);

for (int i = 0; i < responses.Count; i++)
{
    if (responses[i].IsSuccess)
    {
        Log.Message($"{texts[i]} -> {responses[i].Content}");
    }
    else
    {
        Log.Error($"翻译失败: {texts[i]} - {responses[i].ErrorMessage}");
    }
}
```

**批量数据分析**
```csharp
var dataQueries = new List<string>
{
    "分析当前殖民地的食物状况",
    "评估殖民地的防御能力", 
    "检查殖民地的心情状态",
    "统计殖民地的资源情况"
};

var options = new LLMRequestOptions
{
    MaxTokens = 300,
    Temperature = 0.5f,
    EnableCaching = true
};

var reports = await RimAIAPI.SendBatchRequestAsync(dataQueries, options);

// 并行处理结果
Parallel.ForEach(reports.Where(r => !string.IsNullOrEmpty(r)), response => {
    ProcessAnalysisReport(response);
});
```

---

### 5. GetFunctionCallAsync - 获取函数调用建议

此高级API允许你向模型提供一系列你定义的工具（函数），并让模型根据用户的`prompt`智能地决定调用哪个工具，以及使用什么参数。这使得构建能够与游戏世界或其他系统进行实际交互的复杂AI代理成为可能。

**核心思想**：你定义工具，AI决定何时以及如何使用它们。

#### 方法签名
```csharp
// 注意：为避免与Verse.Tool冲突，请使用using别名
using AITool = RimAI.Framework.LLM.Models.Tool;

public static async Task<List<FunctionCallResult>> GetFunctionCallAsync(
    string prompt,
    List<AITool> tools,
    CancellationToken cancellationToken = default
)
```

#### 参数详解

**prompt (string, 必需)**
- 用户的输入，模型将基于此内容决定是否调用工具。

**tools (List<AITool>, 必需)**
- 你提供给模型的一系列可用工具。
- 这是一个关键参数，定义了模型的能力边界。

**cancellationToken (CancellationToken, 可选)**
- 用于取消请求。

#### 返回值：`List<FunctionCallResult>`
- 一个包含零个或多个`FunctionCallResult`对象的列表。
- 如果模型认为不需要调用任何工具，或者发生错误，返回`null`。
- `FunctionCallResult`的结构如下：
```csharp
public class FunctionCallResult
{
    public string ToolId { get; set; }       // 工具调用的唯一ID
    public string FunctionName { get; set; } // 建议调用的函数名
    public string Arguments { get; set; }    // JSON格式的函数参数字符串
}
```

#### 数据模型：定义一个工具 (Tool)
一个工具由其类型（始终是 "function"）和一个函数定义组成。

```csharp
// AITool, FunctionDefinition等模型均在
// RimAI.Framework.LLM.Models 命名空间下

var myTool = new AITool
{
    Type = "function",
    Function = new FunctionDefinition
    {
        Name = "your_function_name", // 函数的唯一名称
        Description = "这个函数是做什么的清晰描述", // 模型会根据描述来决定何时使用它
        Parameters = new FunctionParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ParameterProperty>
            {
                // 定义你的参数
                { "param1", new ParameterProperty { Type = "string", Description = "参数1的描述" } },
                { "param2", new ParameterProperty { Type = "number", Description = "参数2的描述" } }
            },
            Required = new List<string> { "param1" } // 必需的参数列表
        }
    }
};
```

#### 使用示例：构建一个游戏内计算器

假设我们想让AI能够回答数学问题，但我们不希望它自己计算，而是调用我们游戏中的精确计算方法。

**第一步：定义工具**
```csharp
using AITool = RimAI.Framework.LLM.Models.Tool;
using RimAI.Framework.LLM.Models; // 引入FunctionDefinition等模型

// 创建一个乘法工具
var multiplyTool = new AITool
{
    Type = "function",
    Function = new FunctionDefinition
    {
        Name = "multiply",
        Description = "计算两个数的乘积",
        Parameters = new FunctionParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ParameterProperty>
            {
                { "a", new ParameterProperty { Type = "number", Description = "第一个数字" } },
                { "b", new ParameterProperty { Type = "number", Description = "第二个数字" } }
            },
            Required = new List<string> { "a", "b" }
        }
    }
};

var tools = new List<AITool> { multiplyTool };
```

**第二步：调用API**
```csharp
var prompt = "请帮我计算一下，如果我有128个单位的白银，每个单位价值5.5元，总价值是多少？";

List<FunctionCallResult> suggestedCalls = await RimAIAPI.GetFunctionCallAsync(prompt, tools);
```

**第三步：处理结果并执行本地方法**
```csharp
if (suggestedCalls != null && suggestedCalls.Count > 0)
{
    foreach (var call in suggestedCalls)
    {
        Log.Message($"AI建议调用函数: {call.FunctionName}");
        Log.Message($"参数 (JSON): {call.Arguments}");

        if (call.FunctionName == "multiply")
        {
            // 你需要一个类来反序列化参数
            // 例如：public class MultiplyArgs { public double a { get; set; } public double b { get; set; } }
            var args = JsonConvert.DeserializeObject<MultiplyArgs>(call.Arguments);
            
            // 执行你自己的本地C#方法
            double result = MyLocalCalculator.Multiply(args.a, args.b);
            
            Log.Message($"本地计算结果: {result}");
            
            // 后续步骤：你可以将执行结果再发给AI，让它以自然语言回复用户。
        }
    }
}
else
{
    Log.Message("AI没有建议调用任何函数。");
}
```

#### 完整示例：多工具选择

这个更复杂的示例展示了如何定义多个工具，并让AI根据不同的用户问题自动选择最合适的一个。

**第一步：定义所有工具**
```csharp
using AITool = RimAI.Framework.LLM.Models.Tool;
using RimAI.Framework.LLM.Models;
using System.Collections.Generic;

var tools = new List<AITool>
{
    // 工具1: 乘法
    new AITool { Function = new FunctionDefinition {
        Name = "mul",
        Description = "计算两个数的乘积",
        Parameters = new FunctionParameters {
            Properties = new Dictionary<string, ParameterProperty> {
                { "a", new ParameterProperty { Type = "number", Description = "第一个数字" } },
                { "b", new ParameterProperty { Type = "number", Description = "第二个数字" } }
            },
            Required = new List<string> { "a", "b" }
        }
    }},
    // 工具2: 比较大小
    new AITool { Function = new FunctionDefinition {
        Name = "compare",
        Description = "比较两个数字的大小",
        Parameters = new FunctionParameters {
            Properties = new Dictionary<string, ParameterProperty> {
                { "a", new ParameterProperty { Type = "number", Description = "第一个数字" } },
                { "b", new ParameterProperty { Type = "number", Description = "第二个数字" } }
            },
            Required = new List<string> { "a", "b" }
        }
    }},
    // 工具3: 统计字符
    new AITool { Function = new FunctionDefinition {
        Name = "count_letter_in_string",
        Description = "统计一个字符串中某个字母出现的次数",
        Parameters = new FunctionParameters {
            Properties = new Dictionary<string, ParameterProperty> {
                { "a", new ParameterProperty { Type = "string", Description = "源字符串" } },
                { "b", new ParameterProperty { Type = "string", Description = "要计数的字母" } }
            },
            Required = new List<string> { "a", "b" }
        }
    }}
};
```

**第二步：针对不同问题调用API**
```csharp
var prompts = new List<string>
{
    "用中文回答：strawberry中有多少个r?",
    "用中文回答：9.11和9.9，哪个小?"
};

foreach (var prompt in prompts)
{
    Log.Message($"\n--- 处理新问题: {prompt} ---");
    var suggestedCalls = await RimAIAPI.GetFunctionCallAsync(prompt, tools);

    if (suggestedCalls != null && suggestedCalls.Count > 0)
    {
        var call = suggestedCalls.First();
        Log.Message($"AI建议调用: '{call.FunctionName}'");
        Log.Message($"参数: {call.Arguments}");
        
        // 在这里，你可以根据call.FunctionName调用你本地的C#方法
        // 并用返回的结果进行下一步操作（比如再次调用AI生成最终回复）
    }
    else
    {
        Log.Message("AI没有建议任何函数调用，可能直接回答了问题。");
        // 或者直接把prompt发给SendMessageAsync获取最终回复
        var directResponse = await RimAIAPI.SendMessageAsync(prompt);
        Log.Message($"AI直接回复: {directResponse}");
    }
}
```

**预期输出**
```
--- 处理新问题: 用中文回答：strawberry中有多少个r? ---
AI建议调用: 'count_letter_in_string'
参数: {"a": "strawberry", "b": "r"}

--- 处理新问题: 用中文回答：9.11和9.9，哪个小? ---
AI建议调用: 'compare'
参数: {"a": 9.11, "b": 9.9}
```

这个功能非常强大，它将AI的自然语言理解能力和你的代码执行能力连接了起来，为创造更智能、更具交互性的MOD功能开辟了新的可能性。

## ⚙️ LLMRequestOptions 详细参数

### 基础参数

```csharp
public class LLMRequestOptions
{
    // 温度控制 (0.0-2.0)
    public float? Temperature { get; set; }
    
    // 最大返回Token数
    public int? MaxTokens { get; set; }
    
    // 是否启用缓存
    public bool EnableCaching { get; set; } = true;
    
    // 请求超时时间(秒)
    public int? TimeoutSeconds { get; set; }
    
    // 重试次数
    public int? RetryCount { get; set; }
    
    // Top-p采样参数
    public float? TopP { get; set; }
    
    // 频率惩罚
    public float? FrequencyPenalty { get; set; }
    
    // 存在惩罚
    public float? PresencePenalty { get; set; }
    
    // 停止词列表
    public List<string> StopWords { get; set; }
    
    // 自定义HTTP头
    public Dictionary<string, string> CustomHeaders { get; set; }
    
    // 用户标识
    public string UserId { get; set; }
}
```

### 参数详解

**Temperature (float?, 0.0-2.0)**
- 控制响应的随机性和创造性
- 0.0: 确定性输出，适合事实性问题
- 0.7: 平衡创造性和准确性，适合一般对话
- 1.0: 更有创造性，适合创意写作
- 2.0: 高度随机，适合头脑风暴

```csharp
// 事实性问题 - 低温度
var factualOptions = new LLMRequestOptions { Temperature = 0.1f };
var response = await RimAIAPI.SendMessageAsync("RimWorld的发布时间是？", factualOptions);

// 创意写作 - 高温度
var creativeOptions = new LLMRequestOptions { Temperature = 1.2f };
var story = await RimAIAPI.SendMessageAsync("写一个科幻短故事", creativeOptions);
```

**MaxTokens (int?)**
- 限制返回内容的长度
- 1 token ≈ 0.75个英文单词 ≈ 0.5个中文字符
- 建议值：50-2000

```csharp
// 简短回答
var shortOptions = new LLMRequestOptions { MaxTokens = 50 };

// 详细回答
var detailedOptions = new LLMRequestOptions { MaxTokens = 1000 };
```

**EnableCaching (bool)**
- 是否启用响应缓存
- true: 相同请求使用缓存，提高性能
- false: 每次都发送新请求

#### 🔍 深度解析：缓存相似性判断机制

**缓存键生成算法**：
```
缓存键 = LLM:{消息哈希}:temp={Temperature}:maxtok={MaxTokens}:model={Model}:json={JsonMode}:schema={SchemaHash}:topp={TopP}:...
```

**影响缓存命中的参数**：
- **消息内容**：使用 `GetHashCode()` 计算文本哈希
- **Temperature**：创造性参数，必须完全匹配
- **MaxTokens**：最大令牌数，必须一致
- **Model**：AI模型名称
- **ForceJsonMode**：是否强制JSON输出
- **JsonSchema**：JSON架构的哈希值
- **TopP**：Top-p采样参数
- **其他参数**：FrequencyPenalty、PresencePenalty等

**相似性判断示例**：
```csharp
// ✅ 这些请求会被判断为相同（缓存命中）
var req1 = new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 500 };
var req2 = new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 500 };
// 缓存键相同：LLM:12345678:temp=0.7:maxtok=500:model=default:json=False

// ❌ 这些请求被判断为不同（缓存未命中）
var req3 = new LLMRequestOptions { Temperature = 0.8f, MaxTokens = 500 };
// 缓存键不同：LLM:12345678:temp=0.8:maxtok=500:model=default:json=False

var req4 = new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 600 };
// 缓存键不同：LLM:12345678:temp=0.7:maxtok=600:model=default:json=False
```

**缓存优化策略**：
```csharp
// ✅ 标准化配置提高命中率
public static class StandardOptions
{
    public static readonly LLMRequestOptions Creative = new LLMRequestOptions 
    { 
        Temperature = 1.0f, MaxTokens = 800, EnableCaching = true 
    };
    
    public static readonly LLMRequestOptions Factual = new LLMRequestOptions 
    { 
        Temperature = 0.2f, MaxTokens = 500, EnableCaching = true 
    };
}

// ✅ 重复使用标准配置
var response1 = await RimAIAPI.SendMessageAsync("问题1", StandardOptions.Factual);
var response2 = await RimAIAPI.SendMessageAsync("问题2", StandardOptions.Factual);
// 如果"问题1"和"问题2"相同，会命中缓存
```

**缓存生命周期**：
- **默认TTL**：30分钟
- **LRU清理**：当缓存条目超过最大数量时，清理最少使用的条目
- **过期清理**：每2分钟自动清理过期条目
- **内存监控**：估算内存使用，防止内存泄漏

**TimeoutSeconds (int?)**
- 请求超时时间
- 默认：30秒
- 建议范围：5-120秒

**RetryCount (int?)**
- 失败时的重试次数
- 默认：3次
- 建议范围：1-5次

## 🏭 Options工厂方法

框架提供预设的配置选项，简化常用场景的配置：

### RimAIAPI.Options 静态工厂

```csharp
// 创意模式 - 高温度，适合创意内容
var creative = RimAIAPI.Options.Creative();
// 等同于: new LLMRequestOptions { Temperature = 1.0f, MaxTokens = 800 }

// 事实模式 - 低温度，适合事实问答
var factual = RimAIAPI.Options.Factual();  
// 等同于: new LLMRequestOptions { Temperature = 0.2f, MaxTokens = 500 }

// 结构化输出 - 适合JSON输出
var structured = RimAIAPI.Options.Structured();
// 等同于: new LLMRequestOptions { Temperature = 0.3f, MaxTokens = 1000 }

// 流式优化 - 适合流式响应
var streaming = RimAIAPI.Options.Streaming();
// 等同于: new LLMRequestOptions { EnableCaching = false, MaxTokens = 1500 }
```

### 工厂方法使用示例

```csharp
// 创意写作
var story = await RimAIAPI.SendMessageAsync(
    "写一个关于太空殖民的故事", 
    RimAIAPI.Options.Creative()
);

// 事实查询
var info = await RimAIAPI.SendMessageAsync(
    "RimWorld中医疗系统是如何工作的？", 
    RimAIAPI.Options.Factual()
);

// 结构化数据
var json = await RimAIAPI.SendMessageAsync(
    "以JSON格式返回当前殖民地状态", 
    RimAIAPI.Options.Structured()
);
```

## 📊 统计和监控API

### GetStatistics - 获取框架统计信息

#### 方法签名
```csharp
public static FrameworkStatistics GetStatistics()
```

#### 返回值：FrameworkStatistics
```csharp
public class FrameworkStatistics
{
    public int TotalRequests { get; set; }        // 总请求数
    public int SuccessfulRequests { get; set; }  // 成功请求数
    public int FailedRequests { get; set; }      // 失败请求数
    public double AverageResponseTime { get; set; } // 平均响应时间(ms)
    public int CacheHits { get; set; }           // 缓存命中次数
    public int CacheMisses { get; set; }         // 缓存未命中次数
    public double CacheHitRate { get; set; }     // 缓存命中率
    public long TotalTokensUsed { get; set; }    // 总消耗Token数
    public DateTime LastRequestTime { get; set; } // 最后请求时间
    public bool IsHealthy { get; set; }          // 系统健康状态
}
```

#### 使用示例

```csharp
// 获取统计信息
var stats = RimAIAPI.GetStatistics();

Log.Message($"=== RimAI Framework 统计信息 ===");
Log.Message($"总请求数: {stats.TotalRequests}");
Log.Message($"成功率: {(stats.SuccessfulRequests * 100.0 / stats.TotalRequests):F1}%");
Log.Message($"平均响应时间: {stats.AverageResponseTime:F2}ms");
Log.Message($"缓存命中率: {stats.CacheHitRate:P2}");
Log.Message($"总消耗Token: {stats.TotalTokensUsed:N0}");
Log.Message($"系统健康: {(stats.IsHealthy ? "正常" : "异常")}");

// 性能监控
if (stats.AverageResponseTime > 5000)
{
    Log.Warning("响应时间过长，建议检查网络连接");
}

if (stats.CacheHitRate < 0.1)
{
    Log.Warning("缓存命中率过低，建议检查缓存配置");
}
```

### ClearCache - 清理缓存

#### 方法签名
```csharp
public static void ClearCache()
```

#### 使用场景

```csharp
// 定期清理缓存
if (stats.CacheHits + stats.CacheMisses > 1000)
{
    RimAIAPI.ClearCache();
    Log.Message("缓存已清理");
}

// 内存压力时清理
var memoryUsage = GC.GetTotalMemory(false);
if (memoryUsage > 100 * 1024 * 1024) // 超过100MB
{
    RimAIAPI.ClearCache();
    GC.Collect();
}
```

## 🔧 高级用法和最佳实践

### 📦 智能缓存机制深度解析

#### 缓存键构建算法

RimAI Framework使用复合缓存键来精确识别相似请求：

```csharp
// 内部缓存键生成逻辑（简化版）
private string GenerateCacheKey(string prompt, LLMRequestOptions options)
{
    var keyBuilder = new StringBuilder();
    keyBuilder.Append("LLM:");
    keyBuilder.Append(prompt?.GetHashCode().ToString() ?? "null");
    
    if (options != null)
    {
        keyBuilder.Append($":temp={options.Temperature}");
        keyBuilder.Append($":maxtok={options.MaxTokens}"); 
        keyBuilder.Append($":model={options.Model ?? "default"}");
        keyBuilder.Append($":json={options.ForceJsonMode}");
        
        if (options.JsonSchema != null)
            keyBuilder.Append($":schema={options.JsonSchema.GetHashCode()}");
        if (options.TopP.HasValue)
            keyBuilder.Append($":topp={options.TopP}");
    }
    
    return keyBuilder.ToString();
}
```

#### 缓存命中条件

**必须完全匹配的参数**：
1. **消息内容**：字符串完全相同
2. **Temperature**：精确到小数点
3. **MaxTokens**：数值完全匹配
4. **Model**：模型名称字符串匹配
5. **ForceJsonMode**：布尔值匹配
6. **JsonSchema**：架构哈希值匹配
7. **TopP**：如果设置，必须精确匹配

#### 实际缓存测试

```csharp
// 测试相似性判断
public async Task TestCacheSimilarity()
{
    var stats = RimAIAPI.GetStatistics();
    var initialHits = stats.CacheHits;
    
    // 第一次请求 - 缓存未命中
    var response1 = await RimAIAPI.SendMessageAsync("Hello World", 
        new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 100 });
    
    // 第二次相同请求 - 应该缓存命中
    var response2 = await RimAIAPI.SendMessageAsync("Hello World", 
        new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 100 });
    
    // 验证缓存命中
    var newStats = RimAIAPI.GetStatistics();
    var cacheHitIncrease = newStats.CacheHits - initialHits;
    
    Log.Message($"缓存命中次数增加: {cacheHitIncrease}");
    Log.Message($"第二次请求来自缓存: {response2.FromCache}");
}
```

#### 缓存优化策略

**策略1：参数标准化**
```csharp
// ✅ 定义标准参数集合
public static class CacheOptimizedOptions
{
    // 事实查询 - 低温度，高缓存命中
    public static readonly LLMRequestOptions Facts = new LLMRequestOptions
    {
        Temperature = 0.2f,
        MaxTokens = 300,
        EnableCaching = true
    };
    
    // 创意内容 - 适中温度，平衡创意性和缓存
    public static readonly LLMRequestOptions Creative = new LLMRequestOptions
    {
        Temperature = 0.8f,
        MaxTokens = 800,
        EnableCaching = true
    };
    
    // 结构化数据 - 固定格式，高缓存价值
    public static readonly LLMRequestOptions Structured = new LLMRequestOptions
    {
        Temperature = 0.1f,
        MaxTokens = 1000,
        ForceJsonMode = true,
        EnableCaching = true
    };
}
```

**策略2：消息模板化**
```csharp
// ✅ 使用模板提高相似性
public class MessageTemplates
{
    public static string AnalyzeColony(string dataType) =>
        $"分析当前殖民地的{dataType}状况，提供详细报告";
    
    public static string TranslateText(string text, string targetLang) =>
        $"将以下文本翻译成{targetLang}：{text}";
}

// 使用模板 - 提高缓存命中
var analysis1 = await RimAIAPI.SendMessageAsync(
    MessageTemplates.AnalyzeColony("食物"), 
    CacheOptimizedOptions.Facts
);

var analysis2 = await RimAIAPI.SendMessageAsync(
    MessageTemplates.AnalyzeColony("防御"), 
    CacheOptimizedOptions.Facts  // 相同参数，其他部分可能命中缓存
);
```

**策略3：缓存预热**
```csharp
// ✅ 预热常用请求缓存
public async Task PrewarmCache()
{
    var commonQueries = new[]
    {
        "当前游戏状态如何？",
        "有什么建议？",
        "分析当前情况",
        "下一步应该做什么？"
    };
    
    foreach (var query in commonQueries)
    {
        // 预热缓存，忽略结果
        _ = await RimAIAPI.SendMessageAsync(query, CacheOptimizedOptions.Facts);
    }
    
    Log.Message("缓存预热完成");
}
```

### 1. 异步处理最佳实践

```csharp
// ✅ 正确：使用ConfigureAwait(false)
public async Task ProcessAIRequestAsync(string message)
{
    var response = await RimAIAPI.SendMessageAsync(message)
        .ConfigureAwait(false);
    
    // 处理响应
    ProcessResponse(response);
}

// ✅ 正确：异常处理
public async Task SafeAIRequestAsync(string message)
{
    try
    {
        var response = await RimAIAPI.SendMessageAsync(message);
        if (!string.IsNullOrEmpty(response))
        {
            // 成功处理
        }
    }
    catch (OperationCanceledException)
    {
        // 操作被取消
    }
    catch (RimAIException ex)
    {
        // RimAI特定异常
        Log.Error($"RimAI异常: {ex.Message}");
    }
    catch (Exception ex)
    {
        // 其他异常
        Log.Error($"未知异常: {ex.Message}");
    }
}
```

### 2. 性能优化技巧

```csharp
// ✅ 缓存优化：相似请求使用缓存
var options = new LLMRequestOptions 
{ 
    EnableCaching = true,
    Temperature = 0.3f // 低温度提高缓存命中率
};

// ✅ 批量处理：减少网络开销
var messages = new List<string> { /* 多个消息 */ };
var responses = await RimAIAPI.SendBatchRequestAsync(messages, options);

// ✅ 超时控制：避免长时间等待
var timeoutOptions = new LLMRequestOptions 
{ 
    TimeoutSeconds = 15,
    RetryCount = 2
};
```

### 3. 内存管理

```csharp
// ✅ 定期清理缓存
public class AIManager
{
    private static DateTime lastCacheClean = DateTime.MinValue;
    
    public async Task<string> ProcessRequestAsync(string message)
    {
        // 每小时清理一次缓存
        if (DateTime.Now - lastCacheClean > TimeSpan.FromHours(1))
        {
            RimAIAPI.ClearCache();
            lastCacheClean = DateTime.Now;
        }
        
        return await RimAIAPI.SendMessageAsync(message);
    }
}
```

### 4. 错误处理策略

```csharp
public async Task<string> ResilientRequestAsync(string message, int maxRetries = 3)
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            var response = await RimAIAPI.SendMessageAsync(message);
            if (!string.IsNullOrEmpty(response))
                return response;
                
            // 失败时等待后重试
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
        catch (Exception ex)
        {
            if (attempt == maxRetries - 1)
                throw; // 最后一次尝试失败，抛出异常
                
            Log.Warning($"请求失败，{attempt + 1}/{maxRetries}，等待重试: {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
    
    throw new InvalidOperationException($"请求在{maxRetries}次尝试后仍然失败");
}
```

## 🚨 常见错误和解决方案

### 1. 网络连接错误
```csharp
// 错误：ConnectionException
// 解决：检查网络连接，增加重试次数
var options = new LLMRequestOptions 
{ 
    TimeoutSeconds = 60,
    RetryCount = 5
};
```

### 2. Token限制错误
```csharp
// 错误：TokenLimitException  
// 解决：减少MaxTokens或分割长消息
var options = new LLMRequestOptions { MaxTokens = 500 };

// 或者分割长消息
if (message.Length > 2000)
{
    var chunks = SplitMessage(message, 2000);
    var responses = await RimAIAPI.SendBatchRequestAsync(chunks);
}
```

### 3. 配置错误
```csharp
// 错误：ConfigurationException
// 解决：检查配置文件和API密钥
try
{
    var response = await RimAIAPI.SendMessageAsync(message);
}
catch (ConfigurationException ex)
{
    Log.Error($"配置错误: {ex.Message}");
    Log.Error("请检查 RimAIConfig.json 文件和API密钥设置");
}
```

---

## 📚 相关文档

- [快速上手指南](CN_v3.0_API快速上手.md) - 快速入门和常用场景
- [功能特性](CN_v3.0_功能特性.md) - 详细功能介绍
- [迁移指南](CN_v3.0_迁移指南.md) - 从v2.x升级指导
- [架构设计](CN_v3.0_架构改造完成报告.md) - 技术架构文档

**RimAI Framework v3.0 - 让AI集成变得简单而强大！** 🚀
