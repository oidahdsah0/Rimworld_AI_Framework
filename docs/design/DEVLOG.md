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

## 下一步计划
1. 实现基础的 AI 聊天功能
2. 开发终端 UI 系统
3. 创建游戏事件监听器
4. 构建上下文管理系统

---

*此问题的解决为整个 RimAI Framework 项目奠定了重要基础。*
