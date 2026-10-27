using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Users;

public sealed class LeaderboardRequest
{
    public Guid UserId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Bimester { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Year { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ModuleNameId { get; set; }
}

public sealed class LeaderboardEntry
{
    public int? Position { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string UserName { get; set; } = "";
    public Guid? ModuleNameId { get; set; }
    public string? ModuleName { get; set; }
    public decimal TotalPoints { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool IsTop10 { get; set; }
}
