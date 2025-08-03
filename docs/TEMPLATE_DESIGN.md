# RimAI.Framework 模板设计 v4 (最终版)

本文档定义了 `RimAI.Framework` 使用的 `provider_template_*.json` 和 `user_config_*.json` 文件的最终结构和设计理念。这是框架实现数据驱动能力的核心，所有相关的 C# 模型类和翻译服务都必须严格遵守此规范。

---

## 1. 核心设计原则

- **数据驱动**: 框架的核心逻辑（如请求构建、响应解析）不应包含任何针对特定供应商的硬编码，所有适配规则均由模板提供。
- **关注点分离**: `provider_template` 定义了 **“如何与API对话”**（公共、非敏感信息），而 `user_config` 定义了 **“我，用户，想如何对话”**（私有、个性化设置）。
- **完全参数化**: 模板必须能够定义API的终结点、认证方式、请求/响应结构、甚至非标准参数，以实现最大程度的灵活性。
- **可扩展性**: 为未来可能出现的、无法预见的需求提供“逃生舱口”（如 `staticParameters`）。

---

## 2. `provider_template_*.json` 结构规范

此文件是 **“API说明书”**，应随Mod一同分发，通常是只读的。

### 2.1 完整示例 (`provider_template_openai.json`)

```json
{
  "providerName": "OpenAI",
  "providerUrl": "https://platform.openai.com/",

  "http": {
    "authHeader": "Authorization",
    "authScheme": "Bearer",
    "headers": {
      "Content-Type": "application/json",
      "OpenAI-Beta": "assistants=v2"
    }
  },

  "chatApi": {
    "endpoint": "https://api.openai.com/v1/chat/completions",
    "defaultModel": "gpt-4o",
    "defaultParameters": {
      "temperature": 0.7,
      "top_p": 1.0
    },
    
    "requestPaths": {
      "model": "model",
      "messages": "messages",
      "temperature": "temperature",
      "topP": "top_p",
      "stream": "stream",
      "tools": "tools",
      "toolChoice": "tool_choice"
    },
    
    "responsePaths": {
      "choices": "choices",
      "content": "message.content",
      "toolCalls": "message.tool_calls",
      "finishReason": "finish_reason"
    },
    
    "toolPaths": { 
      "root": "tools",
      "type": "type",
      "functionName": "function.name",
      "functionDescription": "function.description",
      "functionParameters": "function.parameters"
    },

    "jsonMode": {
      "path": "response_format",
      "value": { "type": "json_object" }
    }
  },

  "embeddingApi": {
    "endpoint": "https://api.openai.com/v1/embeddings",
    "defaultModel": "text-embedding-3-small",
    "maxBatchSize": 2048,

    "requestPaths": {
      "model": "model",
      "input": "input"
    },

    "responsePaths": {
      "dataList": "data",
      "embedding": "embedding",
      "index": "index"
    }
  },

  "staticParameters": {
    "some_static_root_field": "some_value"
  }
}
```

### 2.2 字段详解

- **`providerName`, `providerUrl`**: (string) 提供商的名称和官方网站，用于UI显示。
- **`http`**: (object) 定义所有与HTTP协议相关的配置。
  - `authHeader`: (string) 用于认证的HTTP头名称（如 `Authorization`, `X-Api-Key`）。
  - `authScheme`: (string) 认证方案，会作为用户API Key的前缀（如 `Bearer `）。
  - `headers`: (object) 一个键值对集合，表示所有请求都应包含的通用HTTP头。
- **`chatApi` / `embeddingApi`**: (object) 分别定义Chat和Embedding功能的适配规则。
  - `endpoint`: (string) API的完整URL。
  - `defaultModel`: (string) 用户未指定时使用的默认模型。
  - `maxBatchSize`: (int,仅Embedding) API允许的单次最大输入数量。
  - `defaultParameters`: (object,仅Chat) API的默认参数，如 `temperature`。
  - **`requestPaths`**: (object) **核心字段**。定义了如何将我们的内部请求模型映射到API的JSON请求体。键是框架内部的参数名，值是API期望的JSON字段名。
  - **`responsePaths`**: (object) **核心字段**。定义了如何从API的JSON响应中提取数据。键是框架内部的数据名，值是数据在JSON中的路径（支持`.`符号进行嵌套访问）。
  - **`toolPaths`**: (object,仅Chat) **核心字段**。详细定义了`Function Calling`工具的JSON结构。
  - **`jsonMode`**: (object,仅Chat) **核心字段**。定义了当请求强制JSON输出时，需要在请求体中添加的字段路径和具体值。
- **`staticParameters`**: (object) **“逃生舱口”**。一个JSON对象，其内容会**原封不动地**合并到每个请求体的根级别。用于支持非标准、厂商特有的字段。

---

## 3. `user_config_*.json` 结构规范

此文件是 **“用户的个人设置”**，由用户自行管理或通过Mod设置界面生成。

### 3.1 完整示例 (`user_config_openai.json`)

```json
{
  "apiKey": "sk-...", 

  "chatModelOverride": "gpt-3.5-turbo",
  "embeddingModelOverride": null, 

  "chatEndpointOverride": null, 
  "embeddingEndpointOverride": null,

  "temperature": 0.8,
  "topP": null, 

  "concurrencyLimit": 4, 

  "customHeaders": {
    "x-my-custom-header": "some-value"
  },

  "staticParametersOverride": {
    "extra_body": {
      "use_thought": false
    }
  }
}
```

### 3.2 字段详解

- **`apiKey`**: (string) **必需**。用户的私有API密钥。
- **`...Override`**: (string) 所有以`Override`结尾的字段，用于覆盖`provider_template`中对应的默认值。如果值为`null`或字段不存在，则使用模板的默认值。
- **`temperature`, `topP`**: (float?) 用户偏好的模型参数，将覆盖模板的默认参数。
- **`concurrencyLimit`**: (int) 框架级别的设置，用于控制聊天请求的并发数量。
- **`customHeaders`**: (object) 用户的自定义HTTP头。将与模板的`headers`合并，且当键相同时，**用户的值会覆盖模板的值**。
- **`staticParametersOverride`**: (object) 用户的自定义静态参数。将与模板的`staticParameters`进行**深度合并（deep merge）**，且当路径相同时，**用户的值会覆盖模板的值**。
