# RimAI.Framework 模板设计 v4.2 (最终版)

本文档定义了 `RimAI.Framework` 使用的模板文件的最终结构和设计理念。V4.2 架构将不同功能的模板（如 Chat 和 Embedding）明确分离到独立的文件中。所有相关的 C# 模型类和翻译服务都必须严格遵守此规范。

---

## 1. 核心设计原则

- **数据驱动**: 框架的核心逻辑不包含任何针对特定供应商的硬编码，所有适配规则均由模板提供。
- **关注点分离**:
    - **功能分离**: 聊天 (`Chat`) 和嵌入 (`Embedding`) 的模板是完全独立的文件，允许服务商仅实现其中之一。
    - **配置分离**: `provider_template` 定义了 **“如何与API对话”**（公共、非敏感信息），而 `user_config` 定义了 **“我，用户，想如何对话”**（私有、个性化设置）。
- **链接机制**: 不同的模板文件通过共同的 `providerName` 字段在逻辑上关联到同一个服务提供商。
- **完全参数化**: 模板必须能够定义API的终结点、认证方式、请求/响应结构、甚至非标准参数，以实现最大程度的灵活性。

---

## 2. 聊天模板 (`provider_template_chat_*.json`)

此文件是 **“聊天 API 说明书”**，定义了与特定服务商聊天功能相关的所有配置。

### 2.1 示例 (`provider_template_chat_openai.json`)

```json
{
  "providerName": "OpenAI",
  "providerUrl": "https://platform.openai.com/",

  "http": {
    "authHeader": "Authorization",
    "authScheme": "Bearer",
    "headers": {
      "Content-Type": "application/json"
    }
  },

  "chatApi": {
    "endpoint": "https://api.openai.com/v1/chat/completions",
    "defaultModel": "gpt-4o",
    "defaultParameters": {
      "temperature": 0.7,
      "top_p": 1.0,
      "typical_p": 1.0,
      "max_tokens": 1024
    },
    
    "requestPaths": {
      "model": "model",
      "messages": "messages",
      "temperature": "temperature",
      "topP": "top_p",
      "typicalP": "typical_p",
      "maxTokens": "max_tokens",
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
      "functionRoot": "function",
      "functionName": "name",
      "functionDescription": "description",
      "functionParameters": "parameters"
    },

    "jsonMode": {
      "path": "response_format",
      "value": { "type": "json_object" }
    }
  },
  
  "staticParameters": {
    "some_static_root_field": "some_value"
  }
}
```

### 2.2 字段详解

- **`providerName`, `providerUrl`**: (string) 提供商的名称和官网，用于UI显示和内部链接。
- **`http`**: (object) 定义所有与HTTP协议相关的配置。
- **`chatApi`**: (object) 定义Chat功能的适配规则。
- **`staticParameters`**: (object) “逃生舱口”，用于添加非标准、厂商特有的根级别字段。

---

## 3. Embedding 模板 (`provider_template_embedding_*.json`)

此文件是 **“Embedding API 说明书”**，定义了与特定服务商 Embedding 功能相关的所有配置。

### 3.1 示例 (`provider_template_embedding_openai.json`)

```json
{
  "providerName": "OpenAI",
  "providerUrl": "https://platform.openai.com/",

  "http": {
    "authHeader": "Authorization",
    "authScheme": "Bearer",
    "headers": {
      "Content-Type": "application/json"
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
  }
}
```

### 3.2 字段详解

- **`providerName`, `providerUrl`, `http`**: 与聊天模板中的定义和功能完全相同。
- **`embeddingApi`**: (object) 定义了 Embedding 功能的适配规则，包括端点、默认模型、最大批量大小以及请求/响应路径映射。

---

## 4. 用户配置 (User Config)

用户配置文件同样按功能进行拆分。

### 4.1 聊天用户配置 (`user_config_chat_*.json`)

此文件是 **“用户的个人聊天设置”**。

**示例 (`user_config_chat_openai.json`)**
```json
{
  "apiKey": "sk-...", 

  "modelOverride": "gpt-3.5-turbo",
  "endpointOverride": null,

  "temperature": 0.8,
  "topP": null,
  "typicalP": 1.0,
  "maxTokens": 2048,

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

**字段详解**
- **`apiKey`**: (string) **必需**。用户的私有API密钥。
- **`modelOverride`, `endpointOverride`**: (string) 用于覆盖模板中的默认值。
- **`temperature`, `topP`, `typicalP`, `maxTokens`**: (float? / int?) 用户偏好的模型参数。
- **`concurrencyLimit`**: (int) 框架级别的设置，用于控制聊天请求的并发数量。
- **`customHeaders`, `staticParametersOverride`**: 用于覆盖模板中的对应字段。

### 4.2 Embedding 用户配置 (`user_config_embedding_*.json`)

此文件是 **“用户的个人 Embedding 设置”**。

**示例 (`user_config_embedding_openai.json`)**
```json
{
  "apiKey": "sk-...", 

  "modelOverride": "text-embedding-3-large",
  "endpointOverride": null,

  "customHeaders": {
    "x-my-custom-header": "some-value"
  }
}
```
**字段详解**
- **`apiKey`, `modelOverride`, `endpointOverride`, `customHeaders`**: 功能与聊天用户配置中的同名字段完全相同，但仅作用于 Embedding 请求。
