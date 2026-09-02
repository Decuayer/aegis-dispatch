namespace SocarDispatch.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string message = "") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> FailureResult(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
