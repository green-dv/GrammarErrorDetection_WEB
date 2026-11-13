using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Gamification;

public sealed class ChallengeCreateRequest
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Instructions { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public int Points { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "";
    public bool Active { get; set; }
    public List<GamificationConditionRequest> Conditions { get; set; } = [];
}

public sealed class ChallengeUpdateRequest
{
    public Guid Id { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Difficulty { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Points { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? StartDate { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? EndDate { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Active { get; set; }
    // null conserva las condiciones; una lista las reemplaza (solo en borrador).
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GamificationConditionRequest>? Conditions { get; set; }
}

/// <summary>Nueva ronda: vuelve a habilitar el desafío para todos con nuevas fechas.</summary>
public sealed class ChallengeRepeatRequest
{
    public DateTime StartDate { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? EndDate { get; set; }
}

public sealed class ChallengeResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Instructions { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public int Points { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "";
    public bool Active { get; set; }
    public Guid CreatedByUserId { get; set; }
    // Ronda vigente y estudiantes que la completaron.
    public int Round { get; set; } = 1;
    public long CompletionCount { get; set; }
    public List<GamificationConditionResponse> Conditions { get; set; } = [];
}

