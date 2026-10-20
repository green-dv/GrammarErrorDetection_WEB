using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Users;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IUserService
{
    Task<ApiResponse<List<UserSearchResponse>>?> SearchAsync(UserSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserUpdateResponse>?> UpdateAsync(UserUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<UserLookupOption>>?> GetRolesAsync(int pageNumber, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<UserLookupOption>>?> GetModulesAsync(int pageNumber, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<UserLookupOption>>?> SearchModulesAsync(string? name, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
