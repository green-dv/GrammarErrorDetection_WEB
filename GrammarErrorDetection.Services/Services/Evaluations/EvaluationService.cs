using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Evaluations;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Evaluations;

public sealed class EvaluationService(IApiClient apiClient) : IEvaluationService
{
    public async Task<List<EvaluationResponse>> GetAsync(Guid registrationId, CancellationToken cancellationToken = default)
    {
        if (registrationId == Guid.Empty) throw new ArgumentException("La inscripción no es válida.");
        var results = new Dictionary<Guid, EvaluationResponse>();
        for (var page = 1; ; page++)
        {
            var response = await apiClient.GetAsync<ApiResponse<List<EvaluationResponse>>>(
                $"api/Evaluation?PageNumber={page.ToString(CultureInfo.InvariantCulture)}&PageSize=100&Filter.RegistrationId={registrationId:D}&Filter.Active=true", cancellationToken);
            if (response?.Data is null || response.Error is { ErrorCode: not 0 })
                throw new InvalidOperationException("No se pudieron consultar las evaluaciones.");
            if (response.Data.Any(item => item.RegistrationId != registrationId))
                throw new InvalidOperationException("La API devolvió evaluaciones de otra inscripción.");
            var added = 0;
            foreach (var item in response.Data.Where(item => item.Active))
                if (results.TryAdd(item.Id, item)) added++;
            if (response.Data.Count < 100) break;
            if (added == 0) throw new InvalidOperationException("No se pudo completar la paginación de evaluaciones.");
        }
        return results.Values.ToList();
    }

    public Task<ApiResponse<EvaluationResponse>?> CreateAsync(EvaluationWriteRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostAsync<EvaluationWriteRequest, ApiResponse<EvaluationResponse>>("api/Evaluation", request, cancellationToken);

    public Task<ApiResponse<EvaluationResponse>?> UpdateAsync(EvaluationUpdateRequest request, CancellationToken cancellationToken = default)
        => apiClient.PutAsync<EvaluationUpdateRequest, ApiResponse<EvaluationResponse>>("api/Evaluation", request, cancellationToken);
}
