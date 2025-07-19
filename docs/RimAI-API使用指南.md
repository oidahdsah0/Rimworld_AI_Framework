# RimAI Framework API 使用指南

## 概述

RimAI Framework 是一个为 RimWorld 设计的强大AI框架，提供了完整的大语言模型(LLM)集成功能。该框架支持流式和非流式响应、自定义参数控制、JSON强制格式输出等高级功能。

## 快速开始

### 1. 基本依赖

```csharp
using RimAI.Framework.API;
using RimAI.Framework.LLM.Models;
using System.Threading.Tasks;
```

### 2. 检查框架状态

```csharp
// 检查框架是否已初始化
if (RimAIAPI.IsInitialized)
{
    Log.Message("RimAI Framework 已就绪");
}

// 检查当前流式设置
bool streamingEnabled = RimAIAPI.IsStreamingEnabled;
```

## 核心API功能

### 1. 基础聊天API

#### 简单消息发送

```csharp
// 最简单的消息发送（使用默认设置）
public async void SendSimpleMessage()
{
    string response = await RimAIAPI.SendMessageAsync("描述一个RimWorld殖民者的一天");
    if (response != null)
    {
        Log.Message($"AI回复: {response}");
    }
}
```

#### 带选项的消息发送

```csharp
// 使用自定义选项发送消息
public async void SendMessageWithOptions()
{
    var options = new LLMRequestOptions
    {
        Temperature = 0.8,        // 控制创造性
        MaxTokens = 1000,         // 最大token数
        EnableStreaming = false   // 禁用流式输出
    };

    string response = await RimAIAPI.SendMessageAsync(
        "创造一个有趣的RimWorld事件", 
        options
    );
    
    if (response != null)
    {
        Log.Message($"创意回复: {response}");
    }
}
```

### 2. 流式响应API

#### 基础流式输出

```csharp
// 流式接收响应，逐字显示
public async void SendStreamingMessage()
{
    await RimAIAPI.SendStreamingMessageAsync(
        "讲述一个殖民者的冒险故事",
        chunk => 
        {
            // 每收到一个文字块就调用此回调
            Log.Message($"收到片段: {chunk}");
            // 这里可以更新UI，实时显示内容
        }
    );
}
```

#### 带选项的流式输出

```csharp
// 使用自定义选项的流式输出
public async void SendStreamingWithOptions()
{
    var options = RimAIAPI.Options.Creative(temperature: 1.1); // 高创造性

    await RimAIAPI.SendStreamingMessageAsync(
        "描述一个疯狂的RimWorld场景",
        chunk => UpdateStoryUI(chunk), // 更新故事UI
        options
    );
}
```

## 预设选项 (Options)

RimAI Framework 提供了多种预设选项，方便不同场景使用：

### 1. 流式控制选项

```csharp
// 强制使用流式模式
var streamingOptions = RimAIAPI.Options.Streaming(
    temperature: 0.7,
    maxTokens: 1500
);

// 强制使用非流式模式
var nonStreamingOptions = RimAIAPI.Options.NonStreaming(
    temperature: 0.5,
    maxTokens: 800
);
```

### 2. 场景专用选项

```csharp
// 创意写作（高temperature）
var creativeOptions = RimAIAPI.Options.Creative(temperature: 1.2);
string story = await RimAIAPI.SendMessageAsync("写一个科幻故事", creativeOptions);

// 事实查询（低temperature）
var factualOptions = RimAIAPI.Options.Factual(temperature: 0.3);
string info = await RimAIAPI.SendMessageAsync("解释量子物理", factualOptions);

// JSON格式输出
var jsonOptions = RimAIAPI.Options.Json(temperature: 0.7);
string jsonData = await RimAIAPI.SendMessageAsync("以JSON格式返回殖民者信息", jsonOptions);
```

## 高级API功能

### 1. 自定义服务 (Custom Service)

用于完全控制请求参数：

```csharp
public async void UseCustomService()
{
    var customService = RimAIAPI.GetCustomService();
    if (customService != null)
    {
        var request = new CustomRequest
        {
            Model = "gpt-4",
            Messages = new List<object> 
            {
                new { role = "system", content = "你是RimWorld专家" },
                new { role = "user", content = "分析这个殖民地布局" }
            },
            Temperature = 0.8,
            MaxTokens = 2000,
            ResponseFormat = new { type = "json_object" },
            AdditionalParameters = new Dictionary<string, object>
            {
                ["top_p"] = 0.9,
                ["frequency_penalty"] = 0.1
            }
        };

        var response = await customService.SendCustomRequestAsync(request);
        if (response.Error == null)
        {
            Log.Message($"自定义响应: {response.Content}");
        }
    }
}
```

### 2. JSON服务 (JSON Service)

强制返回有效JSON格式：

```csharp
public async void UseJsonService()
{
    var jsonService = RimAIAPI.GetJsonService();
    if (jsonService != null)
    {
        var options = new LLMRequestOptions { Temperature = 0.5 };
        
        // 泛型JSON响应
        var response = await jsonService.SendJsonRequestAsync<ColonistData>(
            "返回一个殖民者的详细信息",
            options
        );

        if (response.Success)
        {
            ColonistData colonist = response.Data;
            Log.Message($"殖民者: {colonist.Name}, 年龄: {colonist.Age}");
        }
    }
}

// 数据模型示例
public class ColonistData
{
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Skills { get; set; }
    public string Background { get; set; }
}
```

### 3. Mod服务 (Mod Service)

为其他mod提供增强集成：

```csharp
public async void UseModService()
{
    var modService = RimAIAPI.GetModService();
    if (modService != null)
    {
        // Mod特定的增强功能
        var response = await modService.ProcessModRequest("殖民者心理分析");
        Log.Message($"Mod服务响应: {response}");
    }
}
```

## 实用工具方法

### 1. 连接测试

```csharp
public async void TestAPIConnection()
{
    var (success, message) = await RimAIAPI.TestConnectionAsync();
    
    if (success)
    {
        Log.Message($"连接成功: {message}");
    }
    else
    {
        Log.Error($"连接失败: {message}");
    }
}
```

### 2. 请求管理

```csharp
// 取消所有进行中的请求
public void CancelAllRequests()
{
    RimAIAPI.CancelAllRequests();
    Log.Message("已取消所有AI请求");
}

// 刷新设置（当用户更改配置时）
public void RefreshSettings()
{
    RimAIAPI.RefreshSettings();
    Log.Message("已刷新AI设置");
}
```

## 完整示例：智能事件生成器

```csharp
public class SmartEventGenerator
{
    private string currentStory = "";

    // 生成随机事件
    public async Task<string> GenerateRandomEvent()
    {
        var options = RimAIAPI.Options.Creative(temperature: 1.0);
        
        return await RimAIAPI.SendMessageAsync(
            "为RimWorld生成一个有趣的随机事件描述", 
            options
        );
    }

    // 流式生成故事
    public async Task GenerateStoryStream(Action<string> onStoryUpdate)
    {
        currentStory = "";
        var options = RimAIAPI.Options.Streaming(temperature: 0.9, maxTokens: 2000);

        await RimAIAPI.SendStreamingMessageAsync(
            "创作一个关于RimWorld殖民地的长篇故事",
            chunk => 
            {
                currentStory += chunk;
                onStoryUpdate?.Invoke(currentStory);
            },
            options
        );
    }

    // 分析殖民者数据（JSON格式）
    public async Task<ColonistAnalysis> AnalyzeColonist(string colonistInfo)
    {
        var jsonService = RimAIAPI.GetJsonService();
        var options = RimAIAPI.Options.Json(temperature: 0.4);

        var response = await jsonService.SendJsonRequestAsync<ColonistAnalysis>(
            $"分析这个殖民者的信息并返回详细分析: {colonistInfo}",
            options
        );

        return response.Success ? response.Data : null;
    }
}

public class ColonistAnalysis
{
    public string Name { get; set; }
    public string PsychologicalProfile { get; set; }
    public List<string> Strengths { get; set; }
    public List<string> Weaknesses { get; set; }
    public string Recommendation { get; set; }
}
```

## 最佳实践

### 1. 错误处理

```csharp
public async Task<string> SafeAICall(string prompt)
{
    try
    {
        if (!RimAIAPI.IsInitialized)
        {
            Log.Warning("RimAI Framework 未初始化");
            return null;
        }

        var response = await RimAIAPI.SendMessageAsync(prompt);
        return response ?? "AI暂时无响应";
    }
    catch (Exception ex)
    {
        Log.Error($"AI调用失败: {ex.Message}");
        return null;
    }
}
```

### 2. 性能优化

```csharp
// 使用合适的Temperature值
// 0.0-0.3: 事实查询、数据分析
// 0.4-0.7: 平衡创造性和准确性
// 0.8-1.2: 创意写作、故事生成
// 1.3-2.0: 实验性、高度随机

// 控制Token数量避免过长响应
var options = new LLMRequestOptions 
{ 
    MaxTokens = 500,  // 限制响应长度
    Temperature = 0.6 
};
```

### 3. 用户体验

```csharp
// 对于长响应使用流式输出
public async void ShowProgressiveStory()
{
    var storyText = "";
    
    await RimAIAPI.SendStreamingMessageAsync(
        "讲一个长篇RimWorld故事",
        chunk => 
        {
            storyText += chunk;
            // 实时更新UI，让用户看到内容逐渐生成
            UpdateStoryDisplay(storyText);
        }
    );
}
```

## 配置说明

框架的全局设置通过RimWorld的Mod设置界面配置：

- **API Key**: OpenAI或其他兼容API的密钥
- **Endpoint URL**: API端点地址
- **Model Name**: 使用的模型名称
- **Temperature**: 全局创造性参数（0.0-2.0）
- **Enable Streaming**: 全局流式输出开关
- **Enable Embeddings**: 嵌入功能开关

## 注意事项

1. **初始化检查**: 使用API前务必检查 `RimAIAPI.IsInitialized`
2. **异常处理**: 网络请求可能失败，需要适当的错误处理
3. **Token限制**: 注意API的Token使用限制和成本
4. **线程安全**: 所有API都是线程安全的，可以在任何上下文中调用
5. **设置变更**: 修改设置后调用 `RefreshSettings()` 确保生效

---

这个框架为RimWorld modding社区提供了强大且灵活的AI集成能力，无论是简单的文本生成还是复杂的数据分析，都能轻松实现。
