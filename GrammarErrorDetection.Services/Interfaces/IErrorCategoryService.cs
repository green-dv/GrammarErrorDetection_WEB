using GrammarErrorDetection.Models.Categories;
using GrammarErrorDetection.Models.Responses;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IErrorCategoryService
{
    /// <summary>Página de categorías; <see cref="ApiResponse{T}.TotalCount"/> es el total sin paginar.</summary>
    Task<ApiResponse<List<ErrorCategory>>?> SearchAsync(ErrorCategorySearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ErrorCategory>?> CreateAsync(ErrorCategoryCreateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ErrorCategory>?> UpdateAsync(ErrorCategoryUpdateRequest request, CancellationToken cancellationToken = default);
}
