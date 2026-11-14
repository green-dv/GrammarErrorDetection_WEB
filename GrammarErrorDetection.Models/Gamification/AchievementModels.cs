using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Gamification;

public sealed class AchievementCreateRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? AchievementCollectionId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TierOrder { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid AchievementCategoryId { get; set; }
    public int Points { get; set; }
    public bool IsSecret { get; set; }
    public bool Active { get; set; }
    public List<GamificationConditionRequest> Conditions { get; set; } = [];
}

public sealed class AchievementUpdateRequest
{
    public Guid Id { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? AchievementCollectionId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TierOrder { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ClearCollection { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? AchievementCategoryId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Points { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsSecret { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Active { get; set; }
    // null conserva las condiciones; una lista las reemplaza.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GamificationConditionRequest>? Conditions { get; set; }
}

public class AchievementResponse
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? AchievementCollectionId { get; set; }
    public int? TierOrder { get; set; }
    public string? AchievementCollectionName { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid AchievementCategoryId { get; set; }
    public int Points { get; set; }
    public bool IsSecret { get; set; }
    public bool Active { get; set; }
    public string? IconLink { get; set; }
    public Guid? CreatedByUserId { get; set; }
    // En medallas: primera condición pendiente. Conditions trae todas (con avance en medallas).
    public string? ConditionCode { get; set; }
    public string? ConditionName { get; set; }
    public string? AchievementCategoryName { get; set; }
    public List<GamificationConditionResponse> Conditions { get; set; } = [];
}

