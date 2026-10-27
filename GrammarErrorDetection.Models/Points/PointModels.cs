using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Points;

public sealed class PointTransactionRequest
{
    public Guid RegistrationId { get; set; }
    public int Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public DateTime? GrantedAt { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class PointTransactionUpdateRequest
{
    public Guid Id { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Guid? RegistrationId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? Amount { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Reason { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public DateTime? GrantedAt { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? Active { get; set; }
}

public sealed class PointTransactionResponse
{
    public Guid Id { get; set; }
    public Guid RegistrationId { get; set; }
    public int Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; }
}
public sealed class PointTotalResponse { public long TotalPoints { get; set; } }
public sealed class UserLevelResponse
{
    public Guid Id { get; set; }
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public long MinPoints { get; set; }
    public long? MaxPoints { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; }
    public int? TotalCount { get; set; }
}

public sealed record LevelProgress(UserLevelResponse? Current, UserLevelResponse? Next, long TotalPoints)
{
    public long? Remaining => Next is null ? null : Next.MinPoints - TotalPoints;
    public double Percent => Next is null ? 100 : Current is null ? 0
        : Math.Clamp(100d * ((double)TotalPoints - Current.MinPoints) / ((double)Next.MinPoints - Current.MinPoints), 0, 100);
    // Hasta acordar la semántica de maxPoints, el siguiente mínimo define el cambio de nivel.
    public static LevelProgress From(long total, IEnumerable<UserLevelResponse> levels)
    {
        var ordered = levels.Where(x => x.Active).OrderBy(x => x.MinPoints).ToList();
        return new(ordered.LastOrDefault(x => x.MinPoints <= total), ordered.FirstOrDefault(x => x.MinPoints > total), total);
    }
}
