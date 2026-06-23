namespace TenderDocs.Application.Common.Models;

public class Result
{
    public bool Succeeded { get; init; }
    public string[] Errors { get; init; } = Array.Empty<string>();
    public static Result Success() => new() { Succeeded = true };
    public static Result Failure(params string[] errors) => new() { Succeeded = false, Errors = errors };
}

public class Result<T> : Result
{
    public T? Data { get; init; }
    public static Result<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static new Result<T> Failure(params string[] errors) => new() { Succeeded = false, Errors = errors };
}

public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
