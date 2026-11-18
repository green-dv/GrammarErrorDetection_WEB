using GrammarErrorDetection.Models.Points;
using GrammarErrorDetection.Models.Responses;
namespace GrammarErrorDetection.Services.Interfaces;

public interface IPointService
{
    Task<PointTotalResponse> GetTotalAsync(Guid userId, Guid? classId = null, CancellationToken cancellationToken = default);
    /// <summary>Página del historial y total sin paginar (null si la API no lo informa).</summary>
    Task<(List<PointTransactionResponse> Items, long? TotalCount)> GetHistoryPageAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<List<PointTransactionResponse>> GetHistoryAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<List<UserLevelResponse>> GetLevelsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<PointTransactionResponse>?> CreateAsync(PointTransactionRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PointTransactionResponse>?> UpdateAsync(PointTransactionUpdateRequest request, CancellationToken cancellationToken = default);
}
