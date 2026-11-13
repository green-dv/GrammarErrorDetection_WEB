using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Users;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserSearchResponse>?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<UserSearchResponse>>?> SearchAsync(UserSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserUpdateResponse>?> UpdateAsync(UserUpdateRequest request, CancellationToken cancellationToken = default);
    /// <summary>Solo coordinador: crea el usuario con rol y envía una contraseña temporal a su correo.</summary>
    Task<ApiResponse<UserCredentialsResult>?> CreateAsync(UserCreateRequest request, CancellationToken cancellationToken = default);
    /// <summary>Solo coordinador: nueva contraseña temporal por correo y cambio obligatorio.</summary>
    Task<ApiResponse<UserCredentialsResult>?> ResendCredentialsAsync(Guid userId, CancellationToken cancellationToken = default);
    /// <summary>Cambio de la propia contraseña: la API verifica la actual y aplica la política de contraseña segura.</summary>
    Task ChangePasswordAsync(Guid userId, GrammarErrorDetection.Models.Auth.ChangeOwnPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserUpdateResponse>?> UpdateDetailsAsync(UserDetailsUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<UserLookupOption>>?> GetRolesAsync(int pageNumber, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<UserLookupOption>>?> GetModulesAsync(int pageNumber, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<UserLookupOption>>?> SearchModulesAsync(string? name, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
