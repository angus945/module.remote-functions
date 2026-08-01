using RemoteFunctions.Core.Domain;

namespace RemoteFunctions.Core.Application;

public sealed class RemoteFunctionResult<T>
{
    private RemoteFunctionResult(
        bool isSuccess,
        T? data,
        RemoteFunctionError? error)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
    }

    public bool IsSuccess { get; }

    public T? Data { get; }

    public RemoteFunctionError? Error { get; }

    public static RemoteFunctionResult<T> Success(T data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new RemoteFunctionResult<T>(true, data, null);
    }

    public static RemoteFunctionResult<T> Failure(RemoteFunctionError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new RemoteFunctionResult<T>(false, default, error);
    }
}
