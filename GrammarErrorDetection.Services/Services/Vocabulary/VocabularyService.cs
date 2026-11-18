using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Vocabulary;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Vocabulary;

public sealed class VocabularyService(IApiClient apiClient) : IVocabularyService
{
    public Task<ApiResponse<VocabularyAnalysisResponse>?> GetAsync(Guid textId, CancellationToken cancellationToken = default)
    {
        if (textId == Guid.Empty) throw new ArgumentException("El párrafo no es válido.", nameof(textId));
        return apiClient.GetAsync<ApiResponse<VocabularyAnalysisResponse>>($"api/Vocabulary/text/{textId:D}", cancellationToken);
    }
}
