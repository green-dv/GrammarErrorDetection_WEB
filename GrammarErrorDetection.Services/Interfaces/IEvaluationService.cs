using GrammarErrorDetection.Models.Evaluations;
using GrammarErrorDetection.Models.Responses;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IEvaluationService
{
    Task<List<EvaluationResponse>> GetAsync(Guid registrationId, CancellationToken cancellationToken = default);
    Task<ApiResponse<EvaluationResponse>?> CreateAsync(EvaluationWriteRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<EvaluationResponse>?> UpdateAsync(EvaluationUpdateRequest request, CancellationToken cancellationToken = default);
}
