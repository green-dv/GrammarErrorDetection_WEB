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
    private string? errorMessage;

    /// <summary>Traductor de mensajes del servidor (lo registra la presentación al arrancar); sin él se muestra el texto original.</summary>
    public static Func<string, string>? Localize { get; set; }

    public string? ErrorMessage
    {
        get => errorMessage is null ? null : Localize?.Invoke(errorMessage) ?? errorMessage;
        set => errorMessage = value;
    }
    public string? ErrorDetails { get; set; }
}
