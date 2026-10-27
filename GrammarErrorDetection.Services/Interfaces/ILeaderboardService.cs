using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Users;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ILeaderboardService
{
    Task<ApiResponse<List<LeaderboardEntry>>?> GetAsync(LeaderboardRequest request, CancellationToken cancellationToken = default);
}
