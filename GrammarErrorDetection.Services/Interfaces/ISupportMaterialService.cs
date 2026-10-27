using GrammarErrorDetection.Models.Materials;
using GrammarErrorDetection.Models.Responses;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ISupportMaterialService
{
    Task<ApiResponse<List<ErrorClassificationResponse>>?> GetClassificationsAsync(int pageNumber, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<FileTypeResponse>>?> GetFileTypesAsync(int pageNumber, CancellationToken cancellationToken = default);
    Task<ApiResponse<ErrorClassificationResponse>?> GetClassificationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<FileTypeResponse>?> GetFileTypeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<SupportMaterialResponse>>?> SearchAsync(SupportMaterialSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<SupportMaterialResponse>?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<SupportMaterialResponse>?> CreateAsync(SupportMaterialCreateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<SupportMaterialResponse>?> UpdateAsync(SupportMaterialUpdateRequest request, CancellationToken cancellationToken = default);
}
