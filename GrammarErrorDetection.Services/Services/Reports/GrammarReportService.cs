using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Reports;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Reports;

public sealed class GrammarReportService(IApiClient api) : IGrammarReportService
{
    public async Task<GrammarReportData> LoadAsync(GrammarReportScope scope, CancellationToken cancellationToken = default)
    {
        var query = Query(scope);
        var summary = api.GetAsync<ApiResponse<GrammarReportSummary>>($"api/GrammarReport/summary?{query}", cancellationToken);
        var categories = api.GetAsync<ApiResponse<List<GrammarCategoryReport>>>($"api/GrammarReport/categories?{query}", cancellationToken);
        var comparison = api.GetAsync<ApiResponse<List<GrammarReportComparison>>>($"api/GrammarReport/comparison?{query}", cancellationToken);
        await Task.WhenAll(summary, categories, comparison);
        cancellationToken.ThrowIfCancellationRequested();
        var result = new GrammarReportData(Require(await summary), Require(await categories), Require(await comparison));
        if (result.Summary.ClassId != scope.ClassId || result.Summary.UserId != scope.UserId ||
            result.Comparison.Any(row => row.ClassId != scope.ClassId || row.UserId != scope.UserId))
            throw new InvalidOperationException("El reporte recibido no corresponde al curso y usuario solicitados.");
        if (!scope.ClassId.HasValue ? result.Comparison.Count != 0 :
            result.Comparison.Count(row => row.Scope == "Overall") != 1 || result.Comparison.Any(row => row.Scope is not ("Overall" or "Category")))
            throw new InvalidOperationException("La comparación no contiene un resumen general válido.");
        if (result.Categories.Any(row => row.ErrorClassificationId == Guid.Empty) ||
            result.Categories.Select(row => row.ErrorClassificationId).Distinct().Count() != result.Categories.Count ||
            result.Comparison.Where(row => row.Scope == "Category").Any(row => row.ErrorClassificationId is null || row.ErrorClassificationId == Guid.Empty))
            throw new InvalidOperationException("La respuesta contiene categorías inválidas.");
        // Each endpoint has its own transaction. Do not recalculate or reject counts
        // merely because concurrent writes changed totals between these requests.
        return result;
    }
    public Task<GrammarReportFile> ExportAsync(GrammarReportScope scope, CancellationToken cancellationToken = default)
        => DownloadAsync("export", scope, cancellationToken);

    public Task<GrammarReportFile> ExportAffectedAsync(GrammarReportScope scope, CancellationToken cancellationToken = default)
        => DownloadAsync("export/affected", scope, cancellationToken);

    private async Task<GrammarReportFile> DownloadAsync(string endpoint, GrammarReportScope scope, CancellationToken cancellationToken)
    {
        var file = await api.GetFileAsync($"api/GrammarReport/{endpoint}?{Query(scope)}", cancellationToken);
        return new(file.Content, file.FileName, file.ContentType);
    }
    public async Task<IReadOnlyList<StudentProgressRow>> GetStudentProgressAsync(GrammarReportScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.ClassId is null || scope.ClassId == Guid.Empty || scope.UserId == Guid.Empty)
            throw new ArgumentException("Selecciona un curso válido.");
        var query = $"ClassId={scope.ClassId:D}" + (scope.UserId.HasValue ? $"&UserId={scope.UserId:D}" : "");
        var rows = Require(await api.GetAsync<ApiResponse<List<StudentProgressRow>>>($"api/StudentProgress?{query}", cancellationToken));
        if (rows.Any(row => row.UserId == Guid.Empty || row.ClassId == Guid.Empty || row.ClassId == scope.ClassId ||
            (scope.UserId.HasValue && row.UserId != scope.UserId)))
            throw new InvalidOperationException("El historial recibido no corresponde al alcance solicitado.");
        return rows;
    }
    private static string Query(GrammarReportScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if ((scope.ClassId is null && scope.UserId is null) || scope.ClassId == Guid.Empty || scope.UserId == Guid.Empty)
            throw new ArgumentException("Indica al menos un identificador válido de curso o usuario.");
        if (scope.ClassId is null) return $"UserId={scope.UserId:D}";
        return $"ClassId={scope.ClassId:D}" + (scope.UserId.HasValue ? $"&UserId={scope.UserId:D}" : "");
    }
    private static T Require<T>(ApiResponse<T>? response) where T : class => response?.Data is not null && response.Error is not { ErrorCode: not 0 }
        ? response.Data : throw new InvalidOperationException(response?.Error?.ErrorMessage ?? "El servicio no devolvió un reporte válido.");
}
