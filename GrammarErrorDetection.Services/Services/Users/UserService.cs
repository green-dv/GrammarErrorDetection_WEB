using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Users;

public sealed class UserService(IApiClient apiClient) : IUserService
{
    public Task<ApiResponse<List<UserSearchResponse>>?> SearchAsync(UserSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
        if (request.Filter?.ClassId == Guid.Empty) throw new ArgumentException("Selecciona un curso válido.", nameof(request));
        var normalized = new UserSearchRequest { PageNumber = request.PageNumber, PageSize = request.PageSize, Filter = request.Filter?.Normalize() };
        return apiClient.PostAsync<UserSearchRequest, ApiResponse<List<UserSearchResponse>>>("api/User/search", normalized, cancellationToken);
    }

    public Task<ApiResponse<List<UserLookupOption>>?> GetRolesAsync(int pageNumber, CancellationToken cancellationToken = default)
        => apiClient.GetAsync<ApiResponse<List<UserLookupOption>>>($"api/Role?PageNumber={pageNumber}&PageSize=100", cancellationToken);

    public Task<ApiResponse<UserUpdateResponse>?> UpdateAsync(UserUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == Guid.Empty) throw new ArgumentException("Selecciona un usuario válido.", nameof(request));
        if (!request.HasChanges) throw new InvalidOperationException("No hay cambios para guardar.");
        return apiClient.PutAsync<UserUpdateRequest, ApiResponse<UserUpdateResponse>>("api/User", request, cancellationToken);
    }

    public Task<ApiResponse<List<UserLookupOption>>?> GetModulesAsync(int pageNumber, CancellationToken cancellationToken = default)
        => apiClient.GetAsync<ApiResponse<List<UserLookupOption>>>($"api/ModuleName?PageNumber={pageNumber}&PageSize=100", cancellationToken);

    public Task<ApiResponse<UserUpdateResponse>?> UpdateDetailsAsync(UserDetailsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == Guid.Empty) throw new ArgumentException("Selecciona un usuario válido.", nameof(request));
        return apiClient.PutAsync<UserDetailsUpdateRequest, ApiResponse<UserUpdateResponse>>("api/User", request, cancellationToken);
    }

    public Task<ApiResponse<List<UserLookupOption>>?> SearchModulesAsync(string? name, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        var query = $"api/ModuleName?PageNumber={pageNumber}&PageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(name)) query += $"&Filter.Name={Uri.EscapeDataString(name.Trim())}";
        return apiClient.GetAsync<ApiResponse<List<UserLookupOption>>>(query, cancellationToken);
    }
}
