namespace SubscriptionSystem.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public string ErrorMessage { get; }

        private Result(bool isSuccess, T data, string errorMessage)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
        }

        public static Result<T> Success(T data) => new Result<T>(true, data, null);
        public static Result<T> Failure(string errorMessage) => new Result<T>(false, default, errorMessage);
    }
}

