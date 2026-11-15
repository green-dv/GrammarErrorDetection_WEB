using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;

namespace GrammarErrorDetection.Services.Services.Users;

public sealed class LeaderboardService(IApiClient apiClient, AuthSessionState session) : ILeaderboardService
{
    public Task<ApiResponse<List<LeaderboardEntry>>?> GetAsync(LeaderboardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Bimester is < 1 or > 6) throw new ArgumentException("El bimestre debe estar entre 1 y 6.", nameof(request));
        if (request.Year is <= 0) throw new ArgumentException("El año debe ser mayor que cero.", nameof(request));
        if (request.ModuleNameId == Guid.Empty) throw new ArgumentException("Selecciona un módulo válido.", nameof(request));
        var userId = session.Current?.User?.Id;
        if (userId is null || userId == Guid.Empty) throw new InvalidOperationException("Tu sesión expiró. Vuelve a iniciar sesión.");
        var query = new LeaderboardRequest
        {
            UserId = userId.Value,
            Bimester = request.Bimester,
            Year = request.Year,
            ModuleNameId = request.ModuleNameId
        };
        return apiClient.PostAsync<LeaderboardRequest, ApiResponse<List<LeaderboardEntry>>>("api/User/leaderboard", query, cancellationToken);
    }
}
