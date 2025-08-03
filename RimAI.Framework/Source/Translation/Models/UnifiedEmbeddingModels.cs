// =====================================================================================================================
// 文件: UnifiedEmbeddingModels.cs
//
// 作用:
//  此文件定义了 RimAI.Framework 内部用于处理“文本嵌入 (Text Embedding)”功能的“通用语言”。
//  无论我们对接的是 OpenAI、Google 还是任何其他的 AI 服务，框架内部都只使用这里定义的模型。
//  这就像是联合国会议上使用的“官方语言”，所有外部语言（来自不同 API 的数据格式）都必须先翻译成这种
//  官方语言，才能在框架内部流通和处理。
//
//  这种做法的核心好处是“解耦” (Decoupling)，它使得我们的核心逻辑（如 EmbeddingManager）
//  能够独立于任何特定的 API 实现，从而让框架变得更加灵活和易于维护。
//
// 包含的类:
//  - UnifiedEmbeddingRequest:  封装一个向框架发起的 Embedding 请求。
//  - EmbeddingResult:          表示单个文本的 Embedding 计算结果。
//  - UnifiedEmbeddingResponse: 封装框架返回给调用者的、统一格式的 Embedding 响应。
// =====================================================================================================================

// “using” 语句用于引入 C# 的标准库或其他库的功能。
// System.Collections.Generic 包含了我们马上要用到的 List<T> 类型，它是一种非常常用的“泛型列表”或“动态数组”。
using System.Collections.Generic;

// “namespace” (命名空间) 是 C# 用来组织和管理代码的一种方式，可以防止不同代码块之间的命名冲突。
// 我们的命名空间遵循了项目的目录结构，这是一种非常好的实践。
namespace RimAI.Framework.Translation.Models
{
    /// <summary>
    /// 代表一个统一的、向框架发起的 Embedding 请求。
    /// 这是所有 Embedding 操作的起点。
    /// "public" 关键字表示这个类可以被项目中的任何其他代码访问。
    /// </summary>
    public class UnifiedEmbeddingRequest
    {
        /// <summary>
        /// 需要计算 Embedding 的文本列表。
        /// 比如，可以包含 ["你好", "世界", "RimWorld"]。
        /// 
        /// [C# 知识点]:
        ///  - List<string>: 表示这是一个存储“字符串 (string)”的列表。
        ///  - { get; set; }: 这是一个“自动属性 (Auto-Implemented Property)”。
        ///    它是一种简写，编译器会自动为我们创建一个私有的后备字段，并提供公共的 get (读取) 和 set (写入) 方法。
        ///    这让我们可以方便地读取和修改这个属性的值，例如：request.Inputs = new List<string>();
        /// </summary>
        public List<string> Inputs { get; set; }
    }

    /// <summary>
    /// 代表单个输入文本的 Embedding 计算结果。
    /// 一个请求可以包含多个输入文本，因此会产生多个这样的结果。
    /// </summary>
    public class EmbeddingResult
    {
        /// <summary>
        /// 此结果对应于原始输入列表中的索引 (从 0 开始)。
        /// 例如，如果原始输入是 ["你好", "世界"]，那么 Index 为 0 的结果就对应“你好”，Index 为 1 的结果对应“世界”。
        /// 这对于将结果与原始输入重新匹配起来至关重要。
        /// 
        /// [C# 知识点]:
        ///  - int: 表示一个 32 位整数。
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 计算出的 Embedding 向量本身。
        /// 它是一个由浮点数组成的列表，通常包含几百到几千个数字。
        /// 
        /// [C# 知识点]:
        ///  - float: 表示一个单精度浮点数，用于存储带有小数的数字。
        /// </summary>
        public List<float> Embedding { get; set; }
    }

    /// <summary>
    /// 代表一个统一的、由框架返回的 Embedding 响应。
    /// 这是所有 Embedding 操作的最终产出。它将多个 EmbeddingResult 聚合在一起。
    /// </summary>
    public class UnifiedEmbeddingResponse
    {
        /// <summary>
        /// 包含本次请求所有结果的列表。
        /// 列表中的每个元素都是一个 <see cref="EmbeddingResult"/> 对象。
        /// 
        /// [C# 知识点]:
        ///  - <see cref="EmbeddingResult"/>: 这是 XML 文档注释的特殊语法，
        ///    它会在生成的文档中创建一个指向 EmbeddingResult 类的链接，方便开发者快速跳转查看。
        /// </summary>
        public List<EmbeddingResult> Data { get; set; }
    }
}