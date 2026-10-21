using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Texts;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ITextService
{
    Task<ApiResponse<List<TextResponse>>?> SearchAsync(TextSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<TextResponse>?> UploadAsync(TextUploadRequest request, CancellationToken cancellationToken = default);
}
