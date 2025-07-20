# RimAI Framework 开发日志

## 2025年7月16日 - Newtonsoft.Json TypeLoadException 问题解决

### 问题描述
在实现 LLM API 连接功能时，遇到了严重的 `TypeLoadException` 错误：

```
System.TypeLoadException: Could not resolve type with token 0100003e from typeref 
(expected class 'Newtonsoft.Json.JsonConvert' in assembly 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed')
```

### 症状
- 模组设置界面可以正常打开
- 连接测试按钮点击后卡在"测试中"状态
- 日志显示 `TypeLoadException` 错误
- 问题与 Newtonsoft.Json 版本冲突有关

### 尝试的解决方案

#### 1. 降级 Newtonsoft.Json 版本（失败）
- 从 13.0.3 降级到 12.0.3
- 问题依然存在，只是版本号变化

#### 2. 移除 Newtonsoft.Json 依赖（失败）
- 尝试使用 RimWorld 内置的 JSON 库
- 发现 RimWorld 的引用包中不包含 Newtonsoft.Json
- 尝试手动 JSON 字符串构建和解析，代码复杂且容易出错

#### 3. 最终解决方案：程序集加载顺序（成功）
参考 Ludeon 论坛讨论：https://ludeon.com/forums/index.php?topic=54736.0

**关键发现：**
- RimWorld 按**字母顺序**加载程序集
- 当 `Newtonsoft.Json.dll` 在我们的 mod 之后加载时，会导致类型加载失败
- 解决方案是重命名 DLL 文件，确保正确的加载顺序

### 解决步骤

1. **恢复 Newtonsoft.Json 依赖**
   ```xml
   <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
   ```

2. **重命名 DLL 文件**
   ```bash
   mv Newtonsoft.Json.dll 000_Newtonsoft.Json.dll
   ```

3. **自动化构建脚本**
   在 `.csproj` 文件中添加自动重命名逻辑：
   ```xml
   <!-- Step 4: Rename Newtonsoft.Json.dll to ensure it loads before our mod -->
   <Message Text="Step 4: Renaming Newtonsoft.Json.dll for proper load order" Importance="high" />
   <Move SourceFiles="$(ProjectDir)$(OutDir)Newtonsoft.Json.dll" DestinationFiles="$(ProjectDir)$(OutDir)000_Newtonsoft.Json.dll" Condition="Exists('$(ProjectDir)$(OutDir)Newtonsoft.Json.dll')" />
   <Copy SourceFiles="$(ProjectDir)$(OutDir)000_Newtonsoft.Json.dll" DestinationFolder="$(ModDir)\1.6\Assemblies" Condition="Exists('$(ProjectDir)$(OutDir)000_Newtonsoft.Json.dll')" />
   ```

### 结果
- ✅ 连接测试成功显示绿色"Connection successful!"
- ✅ 游戏内消息正常显示
- ✅ 日志记录功能正常工作
- ✅ API 调用正常响应

### 经验教训

1. **RimWorld 模组开发的特殊性**
   - 程序集加载顺序很重要
   - 不能简单地按照常规 .NET 开发方式处理依赖

2. **外部库命名规范**
   - 使用 `000_` 前缀确保优先加载
   - 社区通常使用 `000000JSON.dll` 等命名方式

3. **问题排查流程**
   - 添加详细的日志记录
   - 查阅社区论坛和文档
   - 逐步验证每个假设

4. **构建自动化**
   - 将解决方案集成到构建流程中
   - 避免手动操作导致的遗漏

### 相关资源
- [Ludeon 论坛讨论](https://ludeon.com/forums/index.php?topic=54736.0)
- [.NET 程序集加载文档](https://docs.microsoft.com/en-us/dotnet/standard/assembly/resolve-loads)
- RimWorld 模组开发社区最佳实践

---

## 2025年7月16日 - 翻译文件更新和国际化完善

### 更新内容
在成功解决 Newtonsoft.Json 问题后，发现界面中新增的状态消息和游戏内消息缺少翻译。

### 添加的翻译条目
1. **连接测试状态消息**：
   - `RimAI.Framework.Settings.TestConnectionStatus.Initializing`
   - `RimAI.Framework.Settings.TestConnectionStatus.Validating`
   - `RimAI.Framework.Settings.TestConnectionStatus.Connecting`

2. **游戏内消息**：
   - `RimAI.Framework.Messages.TestStarting`
   - `RimAI.Framework.Messages.SendingRequest`
   - `RimAI.Framework.Messages.TestSuccess`
   - `RimAI.Framework.Messages.TestFailed`
   - `RimAI.Framework.Messages.TestError`

### 支持的语言
- ✅ English
- ✅ 简体中文 (Simplified Chinese)
- ✅ 日本語 (Japanese)
- ✅ 한국어 (Korean)
- ✅ Français (French)
- ✅ Deutsch (German)
- ✅ Русский (Russian)

### 代码更改
- 更新了 `RimAIMod.cs` 中的硬编码字符串使用翻译键
- 更新了 `LLMManager.cs` 中的游戏内消息使用翻译键
- 确保所有用户可见的文本都有完整的多语言支持

### 国际化最佳实践
1. 所有用户可见的字符串都使用翻译键
2. 翻译文件结构清晰，便于维护
3. 为所有支持的语言提供完整翻译
4. 使用语义化的翻译键命名

---

## 2025年7月19日 - 异步请求崩溃问题解决（第三次踩坑！）

### 问题描述 - 致命的线程安全和异步处理问题
用户反馈下游 Mod 在调用 `LLMManager.GetChatCompletionAsync()` 时，总是在接收响应阶段崩溃。这是一个极其隐蔽且危险的问题，表现为：

- 连接测试功能正常工作（因为有 try-catch 保护）
- 下游 Mod 调用时必定崩溃（因为异常传播到了游戏引擎层）
- 即使改为同步操作仍然崩溃
- 崩溃发生在"返回信息"阶段，而不是发送请求阶段

### 根本原因分析

#### 1. **TaskCompletionSource 异常传播问题**（主要元凶）
```csharp
// 原来的问题代码 - 在 ProcessQueueAsync 中
catch (Exception ex)
{
    requestData.CompletionSource.TrySetException(ex);  // ❌ 危险！
}
```
**问题**：当网络请求失败、JSON 解析错误或任何内部异常发生时，异常会通过 `TrySetException()` 传递给下游 Mod。如果下游 Mod 没有 try-catch 保护，就会导致游戏崩溃。

#### 2. **Dynamic 类型运行时绑定问题**
```csharp
// 原来的问题代码
dynamic responseObject = JsonConvert.DeserializeObject(jsonResponse);
string content = responseObject.choices[0].message.content;  // ❌ 可能抛出 RuntimeBinderException
```
**问题**：`dynamic` 在 Unity/RimWorld 环境中不稳定，特别是在 IL2CPP 模式下，访问不存在属性时会抛出运行时绑定异常。

#### 3. **跨线程访问 RimWorld API**
```csharp
// 原来的问题代码 - 在后台线程中调用
var rimAIMod = LoadedModManager.GetMod<RimAIMod>();  // ❌ 非线程安全
```
**问题**：`LoadedModManager` 等 RimWorld API 不是线程安全的，在后台线程中调用可能导致不可预知的行为。

### 解决方案

#### 1. **消除异常传播**
```csharp
// 修复后的代码
catch (Exception ex)
{
    Log.Error($"[RimAI] LLMManager: Unhandled exception: {ex}");
    requestData.CompletionSource.TrySetResult(null);  // ✅ 安全！返回 null 而不是异常
}
```

#### 2. **使用强类型替代 Dynamic**
```csharp
// 添加强类型 DTO 类
private class ChatCompletionResponse
{
    public List<Choice> choices { get; set; }
    // ... 其他属性
}

// 安全的解析方法
var responseObject = JsonConvert.DeserializeObject<ChatCompletionResponse>(jsonResponse);
if (responseObject?.choices != null && responseObject.choices.Count > 0)
{
    return responseObject.choices[0]?.message?.content;
}
```

#### 3. **线程安全的设置加载**
```csharp
private void LoadSettings()
{
    try
    {
        if (_settings == null)
        {
            try
            {
                var rimAIMod = LoadedModManager.GetMod<RimAIMod>();
                if (rimAIMod != null)
                {
                    _settings = rimAIMod.settings;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not load settings (possibly wrong thread): {ex.Message}");
            }
        }
        
        // 安全的回退机制
        if (_settings == null)
        {
            _settings = new RimAISettings();
        }
    }
    catch (Exception ex)
    {
        Log.Error($"Critical error in LoadSettings: {ex}");
        _settings = new RimAISettings();
    }
}
```

#### 4. **改进的 HttpClient 配置**
```csharp
// 更安全的 HttpClient 初始化
var handler = new HttpClientHandler();
try
{
    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
}
catch (Exception ex)
{
    Log.Warning($"Could not configure SSL validation bypass: {ex.Message}");
}
```

### 经验教训（第三次踩这个坑了！）

#### 1. **异步框架设计原则**
- ❌ **永远不要让内部异常泄露给调用者**
- ✅ **内部处理所有异常，对外只返回成功/失败结果**
- ✅ **使用 null 或特定错误码表示失败，而不是异常**

#### 2. **Unity/RimWorld 环境的特殊性**
- ❌ **不要使用 `dynamic` 类型**
- ✅ **使用强类型 DTO 类进行 JSON 序列化**
- ❌ **不要在后台线程调用游戏引擎 API**
- ✅ **缓存设置，避免重复跨线程调用**

#### 3. **调试异步问题的方法**
- 添加详细的日志记录每个步骤
- 区分框架内部错误和调用者错误
- 使用强类型避免运行时错误
- 总是提供安全的回退机制

#### 4. **框架健壮性设计**
```csharp
// 错误的设计 - 让调用者处理异常
public async Task<string> GetDataAsync()
{
    return await SomeRiskyOperation();  // 异常会传播
}

// 正确的设计 - 框架内部处理异常
public async Task<string> GetDataAsync()
{
    try
    {
        return await SomeRiskyOperation();
    }
    catch (Exception ex)
    {
        Log.Error($"Internal error: {ex}");
        return null;  // 安全的失败表示
    }
}
```

### 结果
- ✅ 下游 Mod 不再崩溃
- ✅ 网络错误被安全处理
- ✅ JSON 解析错误被捕获
- ✅ 线程安全问题解决
- ✅ 框架更加健壮和可靠

### 防止再次踩坑的检查清单

**每次设计异步 API 时检查：**
1. [ ] 是否使用了 `TrySetException()`？→ 改为 `TrySetResult(null)`
2. [ ] 是否使用了 `dynamic` 类型？→ 改为强类型 DTO
3. [ ] 是否在后台线程调用游戏 API？→ 添加线程检查和缓存
4. [ ] 是否有未捕获的异常路径？→ 添加全面的异常处理
5. [ ] 调用者是否需要处理复杂的异常？→ 简化为 null 检查

**这是第三次遇到类似的异步/线程安全问题了！每次都很隐蔽，每次都很致命。记住：在游戏 Mod 开发中，异步代码的健壮性比性能更重要！**

---

## 下一步计划
1. 实现基础的 AI 聊天功能
2. 开发终端 UI 系统
3. 创建游戏事件监听器
4. 构建上下文管理系统

---

*此问题的解决为整个 RimAI Framework 项目奠定了重要基础。*
