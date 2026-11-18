using GrammarErrorDetection.Models.Modules;
using GrammarErrorDetection.Models.Responses;

namespace GrammarErrorDetection.Services.Interfaces;

/// <summary>Gestión de módulos (<c>api/ModuleName</c>); el alta y la edición son solo del coordinador.</summary>
public interface IModuleCatalogService
{
    /// <summary>Página de módulos; <see cref="ApiResponse{T}.TotalCount"/> es el total sin paginar.</summary>
    Task<ApiResponse<List<ModuleItem>>?> SearchAsync(ModuleSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ModuleItem>?> CreateAsync(ModuleCreateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ModuleItem>?> UpdateAsync(ModuleUpdateRequest request, CancellationToken cancellationToken = default);
}
