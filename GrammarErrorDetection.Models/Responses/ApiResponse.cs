namespace GrammarErrorDetection.Models.Responses;

public sealed class ApiResponse<T>
{
    public T? Data { get; set; }
    public int? Count { get; set; }
    // Listados paginados con el formato de dos DataTables: total de registros que cumplen los filtros.
    public long? TotalCount { get; set; }
    public ApiError? Error { get; set; }
}

public sealed class ApiError
{
    public int ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
}
