using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Gamification;

public sealed class AchievementCategoryCreateRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Active { get; set; }
}

public sealed class AchievementCategoryUpdateRequest
{
    public Guid Id { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Active { get; set; }
}

public sealed class AchievementCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Active { get; set; }
}

