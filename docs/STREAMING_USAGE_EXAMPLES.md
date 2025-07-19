# 流式返回使用示例

## 概述

RimAI Framework 现在支持流式和非流式两种 LLM 请求方式。流式返回允许实时接收 AI 响应的每个 token 片段，提供更好的用户体验。

**重要：** 在游戏设置中有一个"启用流式传输"选项，当启用时，即使调用 `GetChatCompletionAsync` 也会在内部使用流式传输来提高响应速度！

## 设置选项

### 游戏内设置界面

在 RimWorld 的模组设置中，您会看到：

- ☑️ **启用流式传输** - 当启用时：
  - `GetChatCompletionAsync` 在内部使用流式传输（但仍返回完整响应）
  - `GetChatCompletionStreamAsync` 正常工作，提供实时回调
  - 可能提高响应速度和降低超时风险

- ☐ **启用流式传输** - 当禁用时：
  - `GetChatCompletionAsync` 使用传统的请求-响应模式
  - `GetChatCompletionStreamAsync` 仍然可用（独立工作）

## 使用方法

### 0. 检查流式状态（下游 Mod 重要！）

下游 Mod 可以检查当前的流式设置来调整 UI 行为：

```csharp
// 检查用户是否启用了流式传输
bool isStreamingEnabled = LLMManager.Instance.IsStreamingEnabled;

if (isStreamingEnabled)
{
    // 用户启用了流式，GetChatCompletionAsync 内部使用流式传输
    // UI 可以显示"快速响应模式"或者类似提示
    ShowQuickResponseIndicator();
}
else
{
    // 用户禁用了流式，使用传统模式
    // UI 可以显示"标准模式"提示
    ShowStandardModeIndicator();
}

// 也可以获取完整的设置信息
var settings = LLMManager.Instance.CurrentSettings;
Log.Message($"当前使用模型: {settings.modelName}");
Log.Message($"流式模式: {settings.enableStreaming}");
```

### 实际应用示例：

```csharp
public class SmartAIDialog : Dialog
{
    private void ShowResponseModeInfo()
    {
        if (LLMManager.Instance.IsStreamingEnabled)
        {
            Widgets.Label(infoRect, "🚀 快速响应模式已启用");
        }
        else
        {
            Widgets.Label(infoRect, "📝 标准响应模式");
        }
    }
    
    private async void SendMessage(string message)
    {
        if (LLMManager.Instance.IsStreamingEnabled)
        {
            // 用户启用了流式，可以显示"正在快速获取响应..."
            statusText = "正在快速获取响应...";
        }
        else
        {
            // 传统模式，可能需要更长时间
            statusText = "正在获取响应，请稍候...";
        }
        
        var response = await LLMManager.Instance.GetChatCompletionAsync(message);
        // 处理响应...
    }
}
```

### 1. 非流式请求（现有方式，保持不变）

```csharp
// 传统的一次性返回完整响应
var response = await LLMManager.Instance.GetChatCompletionAsync(
    "Tell me about RimWorld", 
    cancellationToken
);

if (response != null)
{
    Log.Message($"Complete response: {response}");
}
else
{
    Log.Warning("Request failed or returned null");
}
```

### 2. 流式请求（新功能）

```csharp
// 实时接收响应片段
StringBuilder fullResponse = new StringBuilder();

await LLMManager.Instance.GetChatCompletionStreamAsync(
    "Tell me a story about RimWorld colonists",
    chunk =>
    {
        // 每收到一个 token 片段就会调用这个回调
        fullResponse.Append(chunk);
        
        // 可以实时更新 UI 显示部分响应
        UpdateUIWithPartialResponse(fullResponse.ToString());
        
        // 或者逐字符显示效果
        Log.Message($"Received chunk: '{chunk}'");
    },
    cancellationToken
);

Log.Message($"Streaming completed. Full response: {fullResponse}");
```

### 3. 实际游戏场景示例

#### 场景A：AI 助手对话窗口
```csharp
public class AIAssistantDialog : Dialog
{
    private StringBuilder currentResponse = new StringBuilder();
    private string displayText = "";
    
    private async void SendMessage(string userMessage)
    {
        currentResponse.Clear();
        
        await LLMManager.Instance.GetChatCompletionStreamAsync(
            userMessage,
            chunk =>
            {
                currentResponse.Append(chunk);
                displayText = currentResponse.ToString();
                // 触发 UI 重绘
                SetDirty();
            }
        );
    }
    
    public override void DoWindowContents(Rect inRect)
    {
        // 显示实时更新的响应文本
        Widgets.Label(responseRect, displayText);
    }
}
```

#### 场景B：终端式命令响应
```csharp
public class AITerminal
{
    private List<string> terminalLines = new List<string>();
    private StringBuilder currentLine = new StringBuilder();
    
    public async Task ProcessCommand(string command)
    {
        currentLine.Clear();
        terminalLines.Add($"> {command}");
        
        await LLMManager.Instance.GetChatCompletionStreamAsync(
            command,
            chunk =>
            {
                currentLine.Append(chunk);
                
                // 模拟打字机效果
                if (currentLine.Length > 80) // 每行最多80字符
                {
                    terminalLines.Add(currentLine.ToString());
                    currentLine.Clear();
                }
                
                // 更新终端显示
                RefreshTerminalDisplay();
            }
        );
        
        // 添加最后一行
        if (currentLine.Length > 0)
        {
            terminalLines.Add(currentLine.ToString());
        }
    }
}
```

#### 场景D：智能 UI 适配（推荐模式）
```csharp
public class AdaptiveAIInterface
{
    private string statusMessage = "";
    private bool showProgressBar = false;
    
    public async Task ProcessUserInput(string input)
    {
        // 根据流式设置调整 UI 行为
        bool isStreaming = LLMManager.Instance.IsStreamingEnabled;
        
        if (isStreaming)
        {
            // 流式模式：用户期望快速响应，显示简洁的状态
            statusMessage = "🚀 AI 正在快速思考...";
            showProgressBar = false; // 流式模式不需要进度条
            
            // 可以选择使用真正的流式API来提供实时反馈
            var response = new StringBuilder();
            await LLMManager.Instance.GetChatCompletionStreamAsync(
                input,
                chunk => 
                {
                    response.Append(chunk);
                    statusMessage = $"✍️ AI: {response}";
                    RefreshUI();
                }
            );
        }
        else
        {
            // 非流式模式：用户知道需要等待，显示详细进度
            statusMessage = "🤔 AI 正在深度思考，请稍候...";
            showProgressBar = true;
            
            // 显示进度条动画
            StartProgressAnimation();
            
            var response = await LLMManager.Instance.GetChatCompletionAsync(input);
            
            StopProgressAnimation();
            statusMessage = "✅ 响应完成";
            
            if (response != null)
            {
                DisplayFullResponse(response);
            }
        }
    }
    
    private void RefreshUI()
    {
        // 触发界面重绘
        Find.WindowStack.WindowOfType<AIDialog>()?.SetDirty();
    }
}
```

#### 场景E：性能优化的聊天机器人
```csharp
public class PerformanceOptimizedChatBot
{
    public async Task<string> GetAIResponse(string userMessage)
    {
        // 检查用户设置，优化不同模式下的体验
        if (LLMManager.Instance.IsStreamingEnabled)
        {
            // 流式模式：利用内部流式传输的优势
            Log.Message("Using streaming mode for better responsiveness");
            
            // 直接使用 GetChatCompletionAsync，内部会使用流式传输
            return await LLMManager.Instance.GetChatCompletionAsync(userMessage);
        }
        else
        {
            // 非流式模式：可能需要更长的超时时间
            Log.Message("Using traditional mode, allowing longer timeout");
            
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)); // 更长的超时
            return await LLMManager.Instance.GetChatCompletionAsync(userMessage, cts.Token);
        }
    }
}
```
```csharp
public class EventNarrator
{
    public async Task NarrateEvent(string eventContext)
    {
        var narrative = new StringBuilder();
        
        await LLMManager.Instance.GetChatCompletionStreamAsync(
            $"Narrate this RimWorld event: {eventContext}",
            chunk =>
            {
                narrative.Append(chunk);
                
                // 每收到几个词就显示一次消息
                if (narrative.ToString().Split(' ').Length % 10 == 0)
                {
                    Messages.Message(
                        narrative.ToString(),
                        MessageTypeDefOf.NeutralEvent
                    );
                }
            }
        );
        
        // 显示完整的事件描述
        Find.LetterStack.ReceiveLetter(
            "AI Narrator",
            narrative.ToString(),
            LetterDefOf.NeutralEvent
        );
    }
}
```

## 技术细节

### 流式状态检查
下游 Mod 可以通过以下方式了解当前的流式配置：

```csharp
// 检查是否启用流式传输
bool streaming = LLMManager.Instance.IsStreamingEnabled;

// 获取完整的配置信息
var settings = LLMManager.Instance.CurrentSettings;
string model = settings.modelName;
string endpoint = settings.apiEndpoint;
bool embeddings = settings.enableEmbeddings;
```

**重要：** 当 `IsStreamingEnabled` 为 `true` 时，即使调用 `GetChatCompletionAsync` 也会在内部使用流式传输来提高性能。下游 Mod 可以据此调整 UI 提示和用户期望。

### 线程安全
- 流式回调自动在主线程执行，确保 UI 更新安全
- 如果主线程调度失败，会回退到直接调用（带异常保护）

### 错误处理
- 网络错误、JSON 解析错误等都被内部捕获
- 流式处理中的异常不会影响游戏稳定性
- 回调函数中的异常被单独捕获和记录

### 性能考虑
- 流式请求与非流式请求共享相同的并发控制（最多3个同时请求）
- 内存使用优化，避免大响应的一次性加载
- 支持取消操作，可以及时停止长时间的流式响应

### 兼容性
- 完全向后兼容，现有的非流式代码无需修改
- 流式功能是可选的，不使用时不会有任何影响
- 支持所有符合 OpenAI API 标准的 LLM 服务

## 最佳实践

1. **检查流式状态**：在 UI 中根据 `IsStreamingEnabled` 调整用户提示和期望
2. **适当的回调频率**：避免在回调中执行耗时操作
3. **内存管理**：对于长响应，考虑限制显示文本长度
4. **用户体验**：提供停止按钮，允许用户取消长时间的流式请求
5. **错误处理**：始终检查流式请求是否成功完成
6. **UI 更新**：合理控制 UI 更新频率，避免过度重绘
7. **设置感知**：让用户知道当前的响应模式，设置合理的等待期望

### 推荐的下游 Mod 实现模式

```csharp
public class BestPracticeAIIntegration
{
    public async Task ProcessAIRequest(string prompt)
    {
        // 1. 检查当前设置
        bool isStreaming = LLMManager.Instance.IsStreamingEnabled;
        
        // 2. 根据设置调整 UI
        UpdateUIForCurrentMode(isStreaming);
        
        // 3. 选择合适的 API
        if (needRealTimeUpdates && isStreaming)
        {
            // 需要实时更新且启用了流式 - 使用流式 API
            await LLMManager.Instance.GetChatCompletionStreamAsync(prompt, OnChunkReceived);
        }
        else
        {
            // 其他情况 - 使用标准 API（可能内部使用流式）
            var response = await LLMManager.Instance.GetChatCompletionAsync(prompt);
            OnResponseComplete(response);
        }
    }
    
    private void UpdateUIForCurrentMode(bool isStreaming)
    {
        if (isStreaming)
        {
            statusLabel.text = "⚡ 快速响应模式";
            timeoutWarning.SetActive(false);
        }
        else
        {
            statusLabel.text = "📝 标准模式";
            timeoutWarning.SetActive(true);
        }
    }
}
```

## 注意事项

- 流式请求需要 LLM 服务支持 Server-Sent Events (SSE)
- 回调函数应该尽快执行完毕，避免阻塞流处理
- 在回调中修改 UI 时要考虑线程安全
- 流式请求的取消可能不会立即生效，取决于网络状况

这个功能为 RimWorld AI 模组开发提供了更丰富的交互可能性！
