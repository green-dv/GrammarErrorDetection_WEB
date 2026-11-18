using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Vocabulary;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IVocabularyService
{
    /// <summary>Verifica el vocabulario de un párrafo ya analizado. Un estudiante solo accede a sus propios párrafos.</summary>
    Task<ApiResponse<VocabularyAnalysisResponse>?> GetAsync(Guid textId, CancellationToken cancellationToken = default);
}
