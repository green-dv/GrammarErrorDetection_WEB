using GrammarErrorDetection.Models.Gamification;
using GrammarErrorDetection.Models.Responses;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IGamificationService
{
    Task<ApiResponse<List<UserAchievementBestResponse>>?> SearchBestUserAchievementsAsync(UserAchievementBestSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AchievementCollectionResponse>?> CreateAchievementCollectionAsync(AchievementCollectionCreateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AchievementCollectionResponse>?> UpdateAchievementCollectionAsync(AchievementCollectionUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AchievementCollectionResponse>>?> SearchAchievementCollectionsAsync(AchievementCollectionSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AchievementResponse>>?> SearchAchievementsAsync(AchievementSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AchievementResponse>?> CreateAchievementAsync(AchievementCreateRequest request, Stream? image = null, string? fileName = null, string? contentType = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<AchievementResponse>?> UpdateAchievementAsync(AchievementUpdateRequest request, Stream? image = null, string? fileName = null, string? contentType = null, CancellationToken cancellationToken = default);

    Task<ApiResponse<List<ChallengeResponse>>?> SearchChallengesAsync(ChallengeSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ChallengeResponse>?> CreateChallengeAsync(ChallengeCreateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ChallengeResponse>?> UpdateChallengeAsync(ChallengeUpdateRequest request, CancellationToken cancellationToken = default);
    // Nueva ronda con nuevas fechas (vuelve a habilitarlo para todos) y copia en borrador.
    Task<ApiResponse<ChallengeResponse>?> RepeatChallengeAsync(Guid id, ChallengeRepeatRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ChallengeResponse>?> DuplicateChallengeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApiResponse<List<AchievementCategoryResponse>>?> SearchAchievementCategoriesAsync(AchievementCategorySearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AchievementCategoryResponse>?> CreateAchievementCategoryAsync(AchievementCategoryCreateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AchievementCategoryResponse>?> UpdateAchievementCategoryAsync(AchievementCategoryUpdateRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<List<ConditionTypeResponse>>?> SearchConditionTypesAsync(ConditionTypeSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ConditionTypeResponse>?> CreateConditionTypeAsync(ConditionTypeCreateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ConditionTypeResponse>?> UpdateConditionTypeAsync(ConditionTypeUpdateRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<AchievementIconResponse>?> UploadAchievementIconAsync(Stream image, string fileName, string? contentType = null, CancellationToken cancellationToken = default);
}

