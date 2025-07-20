# RimAI Framework API 增强 - 使用指南

## 概述

本次重构将原本庞大的 LLMManager.cs（900+ 行）拆分为多个专门的组件，提供了更强大、更灵活的API接口。

## 主要改进

### 1. 模块化架构
- **LLMManager**: 简化为协调器，负责高级管理
- **LLMExecutor**: 处理实际的HTTP通信
- **LLMRequestQueue**: 管理请求队列和并发控制
- **CustomLLMService**: 提供完全自定义的API调用
- **JsonLLMService**: 强制JSON格式响应
- **ModService**: 增强的Mod服务接口

### 2. 新的API特性
- 流式/非流式可选择
- 自定义请求参数
- JSON模式强制
- Temperature参数控制

## API 使用示例

### 基础API（保持兼容）
```csharp
// 传统方式 - 保持向后兼容
var response = await LLMManager.Instance.GetChatCompletionAsync("Hello, world!");

// 流式传输 - 保持向后兼容
await LLMManager.Instance.GetChatCompletionStreamAsync("Hello!", chunk => {
    Log.Message($"Received: {chunk}");
});
```

### 增强API - 灵活选择流式/非流式
```csharp
// 新的统一接口，可以选择流式或非流式
var options = new LLMRequestOptions 
{
    EnableStreaming = false,  // 或 true
    Temperature = 0.8,
    MaxTokens = 1000
};

var response = await LLMManager.Instance.SendMessageAsync("Hello!", options);

// 或者流式版本
await LLMManager.Instance.SendMessageStreamAsync("Hello!", chunk => {
    // 处理每个chunk
}, options);
```

### 自定义API - 完全控制请求参数
```csharp
var customRequest = new CustomRequest
{
    Model = "gpt-4-turbo",
    Messages = new List<object> 
    {
        new { role = "system", content = "You are a helpful assistant." },
        new { role = "user", content = "Explain quantum physics" }
    },
    Temperature = 0.7,
    MaxTokens = 2000,
    ResponseFormat = new { type = "json_object" },
    AdditionalParameters = new Dictionary<string, object>
    {
        ["top_p"] = 0.9,
        ["frequency_penalty"] = 0.1
    }
};

var customResponse = await LLMManager.Instance.CustomService.SendCustomRequestAsync(customRequest);

if (customResponse.Error == null)
{
    Log.Message($"Response: {customResponse.Content}");
    // 如果是JSON模式，还可以访问 customResponse.JsonContent
}
```

### JSON模式API - 强制JSON响应
```csharp
// 泛型JSON响应
var jsonResponse = await LLMManager.Instance.JsonService.SendJsonRequestAsync<MyDataClass>(
    "Generate a character profile in JSON format"
);

if (jsonResponse.Success)
{
    MyDataClass character = jsonResponse.Data;
    // 使用强类型数据
}

// 带Schema的JSON响应
var schema = new {
    type = "object",
    properties = new {
        name = new { type = "string" },
        age = new { type = "number" },
        skills = new { type = "array", items = new { type = "string" } }
    }
};

var schemaResponse = await LLMManager.Instance.JsonService.SendJsonRequestAsync(
    "Create a character", schema
);
```

### 增强的Mod服务API
```csharp
// 基础Mod服务调用
var modResponse = await LLMManager.Instance.ModService.SendMessageAsync(
    "MyModId", 
    "Generate a random event",
    new LLMRequestOptions { EnableStreaming = true, Temperature = 1.2 }
);

// 流式Mod服务
await foreach (var chunk in LLMManager.Instance.ModService.SendMessageStreamAsync(
    "MyModId", "Tell me a story"))
{
    // 实时显示内容
    MyUI.AppendText(chunk);
}

// 配置Mod特定设置
var modService = LLMManager.Instance.ModService as ModService;
modService?.ConfigureMod(
    "MyModId",
    "You are an AI for a RimWorld colony management mod. Be helpful and strategic.",
    "gpt-4",
    0.7
);
```

## Temperature 参数

新增了Temperature参数到设置UI中：
- **范围**: 0.0 - 2.0
- **推荐**: 0.0 - 1.0
- **说明**:
  - 0.0: 确定性输出，适合需要一致性的场景
  - 0.7: 平衡的创造性（默认值）
  - 1.0: 较高创造性，适合故事生成
  - >1.0: 高度随机，实验性用途

## 流式传输选择

现在每个API调用都可以独立选择是否使用流式传输：

```csharp
// 全局设置仍然有效，但可以被覆盖
var globalStreamingEnabled = LLMManager.Instance.IsStreamingEnabled;

// 但现在可以针对特定请求覆盖
var options = new LLMRequestOptions 
{
    EnableStreaming = !globalStreamingEnabled  // 反向选择
};
```

## JSON模式和流式传输

JSON模式支持流式和非流式两种方式：

```csharp
// 非流式JSON（推荐）
var jsonResponse = await LLMManager.Instance.JsonService.SendJsonRequestAsync<DataModel>(prompt);

// 流式JSON（高级用途）
await foreach (var jsonChunk in LLMManager.Instance.JsonService.SendJsonStreamRequestAsync(prompt))
{
    // 处理JSON片段，需要在客户端组装
}
```

## 迁移指南

### 现有代码兼容性
所有现有的API调用都保持100%兼容，不需要修改：
- `GetChatCompletionAsync()` - 继续工作
- `GetChatCompletionStreamAsync()` - 继续工作  
- `IsStreamingEnabled` - 继续工作
- `TestConnectionAsync()` - 继续工作

### 推荐升级路径
1. **第一阶段**: 继续使用现有API
2. **第二阶段**: 逐步迁移到新的 `SendMessageAsync()` 接口
3. **第三阶段**: 根据需要使用高级功能（Custom/JSON服务）

## 注意事项

### 1. JSON模式注意事项
- JSON模式会在prompt中添加指示，要求LLM返回JSON格式
- 某些模型对JSON格式支持更好
- 建议先测试模型的JSON支持能力

### 2. 流式传输注意事项  
- 流式传输适合长响应和实时交互
- JSON+流式组合需要客户端处理JSON片段组装
- 流式传输在网络不稳定时可能中断

### 3. 性能考虑
- 自定义API允许完全控制，但需要更多配置
- Temperature较高时响应更随机但也更慢
- 并发请求限制仍然存在（默认3个）

## 错误处理

新API提供了更好的错误处理：

```csharp
try 
{
    var response = await LLMManager.Instance.SendMessageAsync(prompt);
    if (response == null) 
    {
        // 处理失败情况
    }
} 
catch (OperationCanceledException) 
{
    // 处理取消
}
catch (Exception ex) 
{
    // 处理其他异常
}
```

这次重构保持了完全的向后兼容性，同时提供了强大的新功能来满足各种高级使用场景。
