using GrammarErrorDetection.Models.ConditionTypes;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.UserLevels;

namespace GrammarErrorDetection.Services.Interfaces;

/// <summary>Gestión de niveles de usuario (<c>api/UserLevel</c>); crear, editar o deshabilitar es solo del coordinador.</summary>
public interface IUserLevelCatalogService
{
    /// <summary>Página de niveles; <see cref="ApiResponse{T}.TotalCount"/> es el total sin paginar.</summary>
    Task<ApiResponse<List<UserLevelItem>>?> SearchAsync(UserLevelSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserLevelItem>?> CreateAsync(UserLevelCreateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserLevelItem>?> UpdateAsync(UserLevelUpdateRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Gestión de tipos de condición (<c>api/ConditionType</c>), solo para el coordinador.</summary>
public interface IConditionTypeCatalogService
{
    /// <summary>Página de tipos; <see cref="ApiResponse{T}.TotalCount"/> es el total sin paginar.</summary>
    Task<ApiResponse<List<ConditionTypeItem>>?> SearchAsync(ConditionTypeSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ConditionTypeItem>?> CreateAsync(ConditionTypeCreateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ConditionTypeItem>?> UpdateAsync(ConditionTypeUpdateRequest request, CancellationToken cancellationToken = default);
}
