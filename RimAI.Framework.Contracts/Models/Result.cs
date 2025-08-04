namespace RimAI.Framework.Contracts
{
    /// <summary>
    /// 非泛型 Result 类型，用于仅表示成功/失败。
    /// </summary>
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

        public static Result Success() => new Result(true, string.Empty);
        public static Result Failure(string error) => new Result(false, error);
    }

    /// <summary>
    /// 带值 Result 类型。
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T Value { get; }
        public string Error { get; }
        public T ErrorPayload { get; }

        private Result(bool isSuccess, T value, string error, T errorPayload = default)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            ErrorPayload = errorPayload;
        }

        public static Result<T> Success(T value) => new Result<T>(true, value, string.Empty);
        public static Result<T> Failure(string error) => new Result<T>(false, default, error);
        public static Result<T> Failure(string error, T errorPayload) => new Result<T>(false, default, error, errorPayload);
    }
}