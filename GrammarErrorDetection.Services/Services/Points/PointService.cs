using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Points;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Points;

public sealed class PointService(IApiClient apiClient) : IPointService
{
    public async Task<PointTotalResponse> GetTotalAsync(Guid userId, Guid? classId = null, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || classId == Guid.Empty) throw new ArgumentException("El usuario o curso no es válido.");
        // Exige usuario para evitar consultar por accidente el total global de la API.
        var path = $"api/PointTransaction/total?UserId={userId:D}" + (classId is { } id ? $"&ClassId={id:D}" : "");
        var response = await apiClient.GetAsync<ApiResponse<PointTotalResponse>>(path, cancellationToken);
        if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException("No se pudo consultar el total de puntos.");
        return response.Data;
    }

    public async Task<List<PointTransactionResponse>> GetHistoryAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("El usuario no es válido.");
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        var response = await apiClient.GetAsync<ApiResponse<List<PointTransactionResponse>>>(
            $"api/PointTransaction?Filter.UserId={userId:D}&PageNumber={pageNumber.ToString(CultureInfo.InvariantCulture)}&PageSize={pageSize.ToString(CultureInfo.InvariantCulture)}", cancellationToken);
        if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException("No se pudo consultar el historial de puntos.");
        // Count indica filas devueltas; no es un total global para calcular páginas.
        return response.Data;
    }

    public async Task<List<UserLevelResponse>> GetLevelsAsync(CancellationToken cancellationToken = default)
    {
        var levels = new Dictionary<Guid, UserLevelResponse>();
        for (var page = 1; ; page++)
        {
            var response = await apiClient.GetAsync<ApiResponse<List<UserLevelResponse>>>(
                $"api/UserLevel?PageNumber={page.ToString(CultureInfo.InvariantCulture)}&PageSize=100&Filter.Active=true", cancellationToken);
            if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException("No se pudo consultar el catálogo de niveles.");
            var added = 0;
            foreach (var level in response.Data)
            {
                if (level.Id == Guid.Empty) throw new InvalidOperationException("El catálogo contiene un nivel sin identificador.");
                if (levels.TryAdd(level.Id, level)) added++;
            }
            var total = response.Data.FirstOrDefault()?.TotalCount;
            if (response.Data.Count == 0 || (total.HasValue ? levels.Count >= total.Value : response.Data.Count < 100)) break;
            if (added == 0) throw new InvalidOperationException("No se pudo completar la paginación de niveles.");
        }
        return levels.Values.Where(x => x.Active).OrderBy(x => x.MinPoints).ToList();
    }

    public Task<ApiResponse<PointTransactionResponse>?> CreateAsync(PointTransactionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RegistrationId == Guid.Empty) throw new ArgumentException("Selecciona una inscripción válida.");
        ValidateReason(request.Reason);
        return apiClient.PostAsync<PointTransactionRequest, ApiResponse<PointTransactionResponse>>("api/PointTransaction", request, cancellationToken);
    }
    public Task<ApiResponse<PointTransactionResponse>?> UpdateAsync(PointTransactionUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Id == Guid.Empty || request.RegistrationId == Guid.Empty) throw new ArgumentException("El movimiento o la inscripción no es válido.");
        if (request.Reason is not null) ValidateReason(request.Reason);
        return apiClient.PutAsync<PointTransactionUpdateRequest, ApiResponse<PointTransactionResponse>>("api/PointTransaction", request, cancellationToken);
    }
    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 255) throw new ArgumentException("El motivo es obligatorio y admite hasta 255 caracteres.");
    }
}
