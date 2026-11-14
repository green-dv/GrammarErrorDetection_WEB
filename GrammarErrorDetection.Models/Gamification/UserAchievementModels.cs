namespace GrammarErrorDetection.Models.Gamification;

// Sin UserId se consulta la cuenta del JWT; UserId permite consultar otro perfil según la autorización de la API.
public sealed class UserAchievementBestSearchRequest
{
    public Guid? UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? AchievementCollectionId { get; set; }
    public Guid? AchievementCategoryId { get; set; }
}

public sealed class UserAchievementDetailResponse : AchievementResponse
{
    public DateTime? ObtainedAt { get; set; }
}

public sealed class UserAchievementBestResponse : AchievementResponse
{
    public string? AchievementCollectionDescription { get; set; }
    public bool IsObtained { get; set; }
    public bool IsMaxTier { get; set; }
    public bool HasNextAchievement { get; set; }
    public Guid? UserAchievementId { get; set; }
    public DateTime? ObtainedAt { get; set; }
    public Guid? PointTransactionId { get; set; }
    public int? AwardPoints { get; set; }
    public UserAchievementDetailResponse? BestAchievement { get; set; }
    public UserAchievementDetailResponse? NextAchievement { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? NextTargetValue { get; set; }
    public string? NextComparisonOperator { get; set; }
    public decimal? NextRequiredValue { get; set; }
    public decimal? RemainingValue { get; set; }
    public decimal? ProgressPercent { get; set; }
    public bool? NextConditionMet { get; set; }
    public bool? CanReachNext { get; set; }
    public long TotalCount { get; set; }
}
