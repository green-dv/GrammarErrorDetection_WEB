using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Users;

public sealed class UserService(IApiClient apiClient) : IUserService
{
    public Task<ApiResponse<UserSearchResponse>?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Selecciona un usuario válido.", nameof(userId));
        return apiClient.GetAsync<ApiResponse<UserSearchResponse>>($"api/User/{userId:D}", cancellationToken);
    }
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

    public Task<ApiResponse<UserCredentialsResult>?> CreateAsync(UserCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RoleId is null || request.RoleId == Guid.Empty) throw new ArgumentException("Selecciona el rol.", nameof(request));
        if (request.BirthDate > DateOnly.FromDateTime(DateTime.Today)) throw new ArgumentException("La fecha de nacimiento no puede ser futura.", nameof(request));
        var normalized = new UserCreateRequest
        {
            FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), CI = request.CI.Trim(),
            Email = request.Email.Trim(), Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(), RoleId = request.RoleId,
            BirthDate = request.BirthDate
        };
        return apiClient.PostAsync<UserCreateRequest, ApiResponse<UserCredentialsResult>>("api/User", normalized, cancellationToken);
    }

    public Task<ApiResponse<UserCredentialsResult>?> ResendCredentialsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Selecciona un usuario válido.", nameof(userId));
        return apiClient.PostAsync<object, ApiResponse<UserCredentialsResult>>($"api/User/{userId:D}/resend-credentials", new { }, cancellationToken);
    }

    public Task<ApiResponse<UserUpdateResponse>?> UpdateAsync(UserUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == Guid.Empty) throw new ArgumentException("Selecciona un usuario válido.", nameof(request));
        if (!request.HasChanges) throw new InvalidOperationException("No hay cambios para guardar.");
        return apiClient.PutAsync<UserUpdateRequest, ApiResponse<UserUpdateResponse>>("api/User", request, cancellationToken);
    }

    public Task<ApiResponse<List<UserLookupOption>>?> GetModulesAsync(int pageNumber, CancellationToken cancellationToken = default)
        => apiClient.GetAsync<ApiResponse<List<UserLookupOption>>>($"api/ModuleName?PageNumber={pageNumber}&PageSize=100", cancellationToken);

    public async Task ChangePasswordAsync(Guid userId, GrammarErrorDetection.Models.Auth.ChangeOwnPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (userId == Guid.Empty) throw new ArgumentException("Selecciona un usuario válido.", nameof(userId));
        if (!GrammarErrorDetection.Models.Auth.PasswordPolicy.IsStrong(request.NewPassword))
            throw new ArgumentException("La nueva contraseña no cumple los requisitos.", nameof(request));
        await apiClient.PutAsync<GrammarErrorDetection.Models.Auth.ChangeOwnPasswordRequest, ApiResponse<bool>>($"api/ChangePassword/{userId:D}", request, cancellationToken);
    }

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
