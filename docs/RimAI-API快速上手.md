# RimAI API 快速上手示例

本文档提供了使用 RimAI Framework API 的实用代码示例，展示了常见的使用模式和最佳实践。

## 基础使用示例

### 示例 1：简单消息发送

```csharp
public async Task SimpleExample()
{
    // 获取 LLM 服务并发送简单消息
    var llmManager = RimAIAPI.GetLLMManager();
    
    var result = await llmManager.GenerateAsync(
        "帮我给这个殖民者取个名字", 
        new RequestOptions()
    );
    
    if (result.Success)
    {
        Log.Message($"AI 响应: {result.Content}");
    }
    else
    {
        Log.Error($"请求失败: {result.ErrorMessage}");
    }
}
```

### 示例 2：流式响应

```csharp
public async Task StreamingExample()
{
    var llmManager = RimAIAPI.GetLLMManager();
    
    await llmManager.GenerateStreamingAsync(
        "给我讲一个关于这个殖民地的故事",
        new RequestOptions 
        { 
            Temperature = 0.8,
            MaxTokens = 200 
        },
        OnPartialResponse  // 回调函数
    );
}

private void OnPartialResponse(string partialResponse)
{
    // 处理流式数据 - 实时更新UI
    UpdateUI(partialResponse);
}
```

### 示例 3：使用 JSON 服务

```csharp
public async Task JsonExample()
{
    var jsonService = RimAIAPI.GetJsonService();
    
    var prompt = @"生成一个具有以下结构的殖民者:
    {
        ""Name"": ""string"",
        ""Age"": number,
        ""Skills"": [
            {""Name"": ""string"", ""Level"": number}
        ]
    }";
    
    var result = await jsonService.GenerateJsonAsync<ColonistData>(
        prompt,
        new JsonRequestOptions { Temperature = 0.7 }
    );
    
    if (result.Success)
    {
        Log.Message($"生成的殖民者: {result.Data.Name}, 年龄: {result.Data.Age}");
    }
}
```

### 示例 4：自定义请求（完全控制）

```csharp
public async Task CustomExample()
{
    var customService = RimAIAPI.GetCustomService();
    var request = new CustomRequest
    {
        Model = "gpt-4",
        Messages = new List<object>
        {
            new { role = "system", content = "你是一个环世界的故事叙述者" },
            new { role = "user", content = "创造一个有趣的事件" }
        },
        Temperature = 0.9,
        MaxTokens = 500,
        Stream = false
    };
    
    var response = await customService.SendCustomRequestAsync(request);
    if (!string.IsNullOrEmpty(response.Error))
    {
        Log.Error($"自定义请求失败: {response.Error}");
    }
}
```

### 示例 5：使用 Mod 服务

```csharp
public async Task ModServiceExample()
{
    var modService = RimAIAPI.GetModService();
    
    // 发送带有 Mod 上下文的消息
    var response = await modService.SendMessageAsync(
        "myModId", 
        "分析一下这种情况",
        new RequestOptions { Temperature = 0.5 }
    );
}
```

## 完整示例类

```csharp
using RimAI.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using Verse;

namespace MyMod
{
    public class RimAIExamples
    {
        // 示例 1：简单消息发送
        public async Task SimpleExample()
        {
            var llmManager = RimAIAPI.GetLLMManager();
            
            var result = await llmManager.GenerateAsync(
                "帮我给这个殖民者取个名字", 
                new RequestOptions()
            );
            
            if (result.Success)
            {
                Log.Message($"AI 响应: {result.Content}");
            }
            else
            {
                Log.Error($"请求失败: {result.ErrorMessage}");
            }
        }

        // 示例 2：流式响应
        public async Task StreamingExample()
        {
            var llmManager = RimAIAPI.GetLLMManager();
            
            await llmManager.GenerateStreamingAsync(
                "给我讲一个关于这个殖民地的故事",
                new RequestOptions 
                { 
                    Temperature = 0.8,
                    MaxTokens = 200 
                },
                OnPartialResponse
            );
        }

        private void OnPartialResponse(string partialResponse)
        {
            // 处理流式数据
            UpdateUI(partialResponse);
        }

        // 示例 3：使用 JSON 服务
        public async Task JsonExample()
        {
            var jsonService = RimAIAPI.GetJsonService();
            
            var prompt = @"生成一个具有以下结构的殖民者:
            {
                ""Name"": ""string"",
                ""Age"": number,
                ""Skills"": [
                    {""Name"": ""string"", ""Level"": number}
                ]
            }";
            
            var result = await jsonService.GenerateJsonAsync<ColonistData>(
                prompt,
                new JsonRequestOptions { Temperature = 0.7 }
            );
            
            if (result.Success)
            {
                Log.Message($"生成的殖民者: {result.Data.Name}, 年龄: {result.Data.Age}");
            }
        }

        // 示例 4：自定义请求（完全控制）
        public async Task CustomExample()
        {
            var customService = RimAIAPI.GetCustomService();
            var request = new CustomRequest
            {
                Model = "gpt-4",
                Messages = new List<object>
                {
                    new { role = "system", content = "你是一个环世界的故事叙述者" },
                    new { role = "user", content = "创造一个有趣的事件" }
                },
                Temperature = 0.9,
                MaxTokens = 500,
                Stream = false
            };
            
            var response = await customService.SendCustomRequestAsync(request);
            if (!string.IsNullOrEmpty(response.Error))
            {
                Log.Error($"自定义请求失败: {response.Error}");
            }
        }

        // 示例 5：使用 Mod 服务
        public async Task ModServiceExample()
        {
            var modService = RimAIAPI.GetModService();
            
            // 发送带有 Mod 上下文的消息
            var response = await modService.SendMessageAsync(
                "myModId", 
                "分析一下这种情况",
                new RequestOptions { Temperature = 0.5 }
            );
        }

        // 辅助方法
        private void UpdateUI(string partialResponse)
        {
            // 在此处更新您的UI
        }
    }

    // JSON 模式的示例数据类
    public class ColonistData
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public List<SkillData> Skills { get; set; }
    }

    public class SkillData
    {
        public string Name { get; set; }
        public int Level { get; set; }
    }
}
```

## 关键要点

1. **始终检查 Success 状态** 在使用结果之前
2. **使用合适的 Temperature 值**：0.0-0.3 适用于事实性内容，0.7-1.0 适用于创意内容
3. **优雅地处理错误** 提供适当的错误日志记录
4. **对于长响应使用流式处理** 以改善用户体验
5. **JSON 模式** 非常适合结构化数据生成
6. **自定义请求** 为您提供对 API 调用的完全控制

## 性能建议

- 尽可能缓存服务实例
- 对于超过几句话的响应使用流式处理
- 设置适当的 MaxTokens 来控制响应长度
- 使用较低的 Temperature 值获得更一致的结果