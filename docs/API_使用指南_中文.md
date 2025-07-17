# RimAI Framework API 使用指南

**版本**: 1.0  
**作者**: [@oidahdsah0](https://github.com/oidahdsah0)  
**更新时间**: 2025年7月

---

## 📋 概述

RimAI Framework 为 RimWorld 模组开发者提供了一个强大而简单的 API，用于与大型语言模型（LLM）进行交互。本框架采用异步队列处理机制，支持并发限制和取消令牌，确保在游戏运行时稳定可靠地调用 AI 服务。

## 🛠️ 快速开始

### 1. 添加依赖

在你的模组项目中，需要添加对 RimAI Framework 的依赖：

#### 在 .csproj 文件中添加引用：

```xml
<ItemGroup>
  <Reference Include="RimAI.Framework">
    <HintPath>path/to/RimAI.Framework.dll</HintPath>
  </Reference>
</ItemGroup>
```

#### 在 About.xml 中添加依赖：

```xml
<ModMetaData>
  <!-- 其他元数据 -->
  <dependencies>
    <li>
      <packageId>oidahdsah0.RimAI.Framework</packageId>
      <displayName>RimAI Framework</displayName>
      <steamWorkshopUrl>steam://url/CommunityFilePage/[workshop_id]</steamWorkshopUrl>
    </li>
  </dependencies>
</ModMetaData>
```

### 2. 导入命名空间

在你的 C# 文件中导入必要的命名空间：

```csharp
using RimAI.Framework.API;
using System.Threading;
using System.Threading.Tasks;
using Verse;
```

## 🎯 核心 API 方法

### GetChatCompletion - 获取聊天完成

这是框架的主要 API 方法，用于向 LLM 发送提示并获取响应。

#### 方法签名

```csharp
public static Task<string> GetChatCompletion(string prompt, CancellationToken cancellationToken = default)
```

#### 参数说明

- `prompt` (string): 发送给 LLM 的文本提示
- `cancellationToken` (CancellationToken, 可选): 用于取消请求的取消令牌

#### 返回值

- `Task<string>`: 异步任务，完成时返回 LLM 的响应字符串，发生错误时返回 null

## 💡 使用示例

### 基础用法

```csharp
public class MyModExample
{
    public async void GenerateBackstory(Pawn pawn)
    {
        try
        {
            string prompt = $"为名为 '{pawn.Name}' 的殖民者生成一个简短而戏剧性的背景故事。";
            string backstory = await RimAIApi.GetChatCompletion(prompt);
            
            if (backstory != null)
            {
                Log.Message($"为 {pawn.Name} 生成的背景故事: {backstory}");
                // 在这里处理生成的背景故事
            }
            else
            {
                Log.Warning("生成背景故事失败");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"生成背景故事时发生错误: {ex.Message}");
        }
    }
}
```

### 使用取消令牌

```csharp
public class MyModExample
{
    public async void GenerateWithTimeout(Pawn pawn)
    {
        // 创建30秒超时的取消令牌
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            try
            {
                string prompt = $"描述 {pawn.Name} 在今天的活动";
                string description = await RimAIApi.GetChatCompletion(prompt, cts.Token);
                
                if (description != null)
                {
                    Log.Message($"活动描述: {description}");
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warning("请求超时被取消");
            }
            catch (Exception ex)
            {
                Log.Error($"请求失败: {ex.Message}");
            }
        }
    }
}
```

### 游戏事件响应

```csharp
[HarmonyPostfix]
[HarmonyPatch(typeof(Pawn_InteractionsTracker), "TryInteractWith")]
public static void OnPawnInteraction(Pawn pawn, Pawn recipient, InteractionDef intDef)
{
    if (intDef.defName == "Insult")
    {
        GenerateInsultResponse(pawn, recipient);
    }
}

private static async void GenerateInsultResponse(Pawn insulter, Pawn target)
{
    string prompt = $"{insulter.Name} 刚刚侮辱了 {target.Name}。" +
                   $"基于 {target.Name} 的性格特点，生成一个合适的反应。";
    
    string response = await RimAIApi.GetChatCompletion(prompt);
    
    if (response != null)
    {
        // 显示反应或触发相应的游戏事件
        Messages.Message($"{target.Name}: {response}", MessageTypeDefOf.NeutralEvent);
    }
}
```

### 批量处理

```csharp
public class ColonyStoryGenerator
{
    public async Task GenerateColonyHistory(List<Pawn> pawns)
    {
        var tasks = new List<Task<string>>();
        
        foreach (var pawn in pawns)
        {
            string prompt = $"为殖民者 {pawn.Name} 生成一个关键的历史时刻";
            tasks.Add(RimAIApi.GetChatCompletion(prompt));
        }
        
        // 等待所有任务完成
        string[] results = await Task.WhenAll(tasks);
        
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] != null)
            {
                Log.Message($"{pawns[i].Name} 的历史: {results[i]}");
            }
        }
    }
}
```

## 🎮 游戏内集成模式

### 1. 事件驱动模式

使用 Harmony 补丁来响应游戏事件，并调用 AI 生成内容：

```csharp
[HarmonyPostfix]
[HarmonyPatch(typeof(RaidStrategyWorker), "TryExecuteWorker")]
public static void OnRaidStart(IncidentParms parms)
{
    GenerateRaidNarrative(parms);
}

private static async void GenerateRaidNarrative(IncidentParms parms)
{
    string prompt = "生成一个关于海盗袭击的紧张描述";
    string narrative = await RimAIApi.GetChatCompletion(prompt);
    
    if (narrative != null)
    {
        Find.LetterStack.ReceiveLetter("袭击警报", narrative, LetterDefOf.ThreatBig);
    }
}
```

### 2. UI 集成模式

在游戏 UI 中添加 AI 生成功能：

```csharp
public class AIStoryDialog : Window
{
    private string currentStory = "";
    private bool isGenerating = false;
    
    public override void DoWindowContents(Rect inRect)
    {
        if (Widgets.ButtonText(new Rect(10, 10, 200, 30), "生成故事"))
        {
            GenerateStory();
        }
        
        Widgets.Label(new Rect(10, 50, inRect.width - 20, inRect.height - 60), currentStory);
    }
    
    private async void GenerateStory()
    {
        if (isGenerating) return;
        
        isGenerating = true;
        currentStory = "正在生成故事...";
        
        string prompt = "生成一个关于太空殖民地的有趣故事";
        string story = await RimAIApi.GetChatCompletion(prompt);
        
        currentStory = story ?? "生成失败";
        isGenerating = false;
    }
}
```

### 3. 定时任务模式

定期生成内容以丰富游戏体验：

```csharp
public class AIStoryManager : GameComponent
{
    private int ticksSinceLastGeneration = 0;
    private const int GenerationInterval = 60000; // 1分钟
    
    public AIStoryManager(Game game) : base(game) { }
    
    public override void GameComponentTick()
    {
        ticksSinceLastGeneration++;
        
        if (ticksSinceLastGeneration >= GenerationInterval)
        {
            GenerateDailyEvent();
            ticksSinceLastGeneration = 0;
        }
    }
    
    private async void GenerateDailyEvent()
    {
        string prompt = "生成一个殖民地的日常小事件";
        string eventText = await RimAIApi.GetChatCompletion(prompt);
        
        if (eventText != null)
        {
            Messages.Message(eventText, MessageTypeDefOf.NeutralEvent);
        }
    }
}
```

## ⚙️ 配置需求

### 用户配置

在使用你的模组之前，用户需要在 RimAI Framework 的设置中配置：

1. **API 密钥**: 用于访问 LLM 服务的认证密钥
2. **API 端点**: LLM 服务的 URL（默认为 OpenAI）
3. **模型名称**: 要使用的 LLM 模型（默认为 gpt-4o）

### 在模组中检查配置

```csharp
public static bool IsAPIReady()
{
    // 检查 API 是否可用
    var testPrompt = "测试";
    var task = RimAIApi.GetChatCompletion(testPrompt);
    
    // 简单检查（实际应用中可能需要更复杂的验证）
    return task != null;
}
```

## ⚠️ 重要提醒：DLL 加载顺序问题

### 问题描述

RimWorld 按照**字母顺序**加载程序集，这可能导致依赖库加载顺序问题。如果你的模组使用了与 RimAI Framework 相同的依赖库（如 Newtonsoft.Json），可能会遇到 `TypeLoadException` 错误。

### 解决方案

1. **确保正确的依赖关系**：在你的 `About.xml` 中正确声明对 RimAI Framework 的依赖
2. **避免重复依赖**：不要在你的模组中包含 RimAI Framework 已经提供的依赖库
3. **使用 Framework 的依赖**：RimAI Framework 已经包含了 `000_Newtonsoft.Json.dll`（重命名以确保优先加载）

### 示例错误和解决

如果你看到类似以下错误：
```
System.TypeLoadException: Could not resolve type with token 0100003e from typeref 
(expected class 'Newtonsoft.Json.JsonConvert' in assembly 'Newtonsoft.Json, Version=13.0.0.0')
```

**解决步骤**：
1. 从你的模组中移除 `Newtonsoft.Json.dll`
2. 确保你的模组在 RimAI Framework 之后加载
3. 在 `About.xml` 中正确声明依赖关系

### 最佳实践

```xml
<!-- 在你的模组的 About.xml 中 -->
<ModMetaData>
  <dependencies>
    <li>
      <packageId>oidahdsah0.RimAI.Framework</packageId>
      <displayName>RimAI Framework</displayName>
      <steamWorkshopUrl>steam://url/CommunityFilePage/[workshop_id]</steamWorkshopUrl>
    </li>
  </dependencies>
</ModMetaData>
```

```xml
<!-- 在你的模组的 .csproj 中，不要包含重复的依赖 -->
<ItemGroup>
  <!-- 正确：只引用 RimAI Framework -->
  <Reference Include="RimAI.Framework">
    <HintPath>path/to/RimAI.Framework.dll</HintPath>
  </Reference>
  
  <!-- 错误：不要包含这个，会导致冲突 -->
  <!-- <PackageReference Include="Newtonsoft.Json" Version="13.0.3" /> -->
</ItemGroup>
```

## 🚨 错误处理

### 常见错误情况

1. **API 密钥未配置**: 返回 null 并记录错误日志
2. **网络连接问题**: 返回 null 并记录相应错误
3. **API 限制**: 自动排队处理，支持并发限制
4. **请求超时**: 可通过取消令牌处理

### 最佳实践

```csharp
public static async Task<string> SafeGetCompletion(string prompt, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                string result = await RimAIApi.GetChatCompletion(prompt, cts.Token);
                
                if (result != null)
                {
                    return result;
                }
                
                // 等待后重试
                await Task.Delay(1000 * (i + 1));
            }
        }
        catch (OperationCanceledException)
        {
            Log.Warning($"请求超时，重试 {i + 1}/{maxRetries}");
        }
        catch (Exception ex)
        {
            Log.Error($"请求失败: {ex.Message}");
        }
    }
    
    return null;
}
```

## 📊 性能考虑

### 并发控制

框架自动限制并发请求数量（默认3个），避免过度调用 API。

### 队列机制

所有请求都通过内部队列处理，确保：
- 请求有序处理
- 避免 API 速率限制
- 系统稳定性

### 内存管理

- 及时释放不需要的字符串
- 使用 `CancellationToken` 取消不需要的请求
- 避免在循环中创建大量异步任务

## 🔧 调试技巧

### 启用详细日志

```csharp
// 在开发时启用详细日志
Log.Message($"发送提示: {prompt}");
string result = await RimAIApi.GetChatCompletion(prompt);
Log.Message($"收到响应: {result ?? "null"}");
```

### 测试 API 连接

```csharp
public static async void TestAPIConnection()
{
    string testPrompt = "请回复'连接成功'";
    
    try
    {
        string response = await RimAIApi.GetChatCompletion(testPrompt);
        Log.Message($"API 测试结果: {response}");
    }
    catch (Exception ex)
    {
        Log.Error($"API 测试失败: {ex.Message}");
    }
}
```

## 🎯 最佳实践建议

1. **优化提示词**: 使用清晰、具体的提示词以获得更好的结果
2. **错误处理**: 始终检查返回值是否为 null
3. **用户体验**: 在 UI 中显示加载状态
4. **性能优化**: 避免频繁的 API 调用
5. **取消支持**: 为长时间运行的操作提供取消选项

## 🔄 未来功能

### 流式响应（计划中）

```csharp
// 未来版本将支持流式响应
await RimAIApi.GetChatCompletionStream(prompt, (chunk) => {
    // 处理每个接收到的文本块
    Log.Message($"接收到: {chunk}");
});
```

---

## 📞 技术支持

如果你在使用过程中遇到问题，请：

1. 检查游戏日志中的错误信息
2. 确认 API 配置正确
3. 在 GitHub 仓库创建 issue
4. 提供详细的错误信息和重现步骤

**GitHub 仓库**: https://github.com/oidahdsah0/Rimworld_AI_Framework

---

*本文档持续更新，请关注最新版本以获取最新功能和修复。*
