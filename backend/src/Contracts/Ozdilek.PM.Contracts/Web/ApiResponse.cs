namespace Ozdilek.PM.Contracts.Web;

/// <summary>Uniform response envelope returned by every service so gateway clients get one predictable shape.</summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<string>? ValidationErrors { get; init; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };

    public static ApiResponse<T> Fail(string error, IReadOnlyList<string>? validationErrors = null) =>
        new() { Success = false, Error = error, ValidationErrors = validationErrors };
}
