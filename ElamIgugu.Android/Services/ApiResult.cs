namespace ElamIguguLeli.App.Services;

public class ApiResult<T>
{
    public bool Ok { get; private init; }
    public T? Data { get; private init; }
    public string? Error { get; private init; }
    public int StatusCode { get; private init; }

    public static ApiResult<T> Success(T data) => new() { Ok = true, Data = data, StatusCode = 200 };
    public static ApiResult<T> Fail(string error, int statusCode = 0) => new() { Ok = false, Error = error, StatusCode = statusCode };
}
