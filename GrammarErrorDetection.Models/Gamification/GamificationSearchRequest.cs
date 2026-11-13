namespace GrammarErrorDetection.Models.Gamification;

public class GamificationSearchRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool? Active { get; set; }
}

public sealed class AchievementSearchRequest : GamificationSearchRequest
{
    public Guid? Id { get; set; }
    public Guid? AchievementCollectionId { get; set; }
    public string? Name { get; set; }
}

public sealed class ChallengeSearchRequest : GamificationSearchRequest
{
    public string? Status { get; set; }
}

public sealed class AchievementCategorySearchRequest : GamificationSearchRequest;

public sealed class AchievementCollectionSearchRequest : GamificationSearchRequest
{
    public Guid? Id { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? Name { get; set; }
}

public sealed class ConditionTypeSearchRequest : GamificationSearchRequest
{
    public string? Code { get; set; }
}

