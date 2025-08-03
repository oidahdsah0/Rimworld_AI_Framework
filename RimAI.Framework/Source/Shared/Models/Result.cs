namespace RimAI.Framework.Shared.Models
{
    // 非泛型 Result 类保持不变
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; }

        protected Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success()
        {
            return new Result(true, string.Empty);
        }

        public static Result Failure(string error)
        {
            return new Result(false, error);
        }
    }

    // 对泛型 Result<T> 类进行增强
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T Value { get; }
        public string Error { get; }

        /// <summary>
        /// 【新增】一个可选的“错误载荷”，用于在失败时携带额外的上下文数据。
        /// 类型是 T，这意味着我们可以返回一个不完整的、但仍有用的 T 对象。
        /// </summary>
        public T ErrorPayload { get; }

        private Result(bool isSuccess, T value, string error, T errorPayload = default)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            ErrorPayload = errorPayload;
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, string.Empty);
        }

        // 这是原有的 Failure 方法，保持不变
        public static Result<T> Failure(string error)
        {
            return new Result<T>(false, default, error);
        }

        // 【新增】Failure 方法的重载版本，允许传入一个“错误载荷”
        public static Result<T> Failure(string error, T errorPayload)
        {
            return new Result<T>(false, default, error, errorPayload);
        }
    }
}