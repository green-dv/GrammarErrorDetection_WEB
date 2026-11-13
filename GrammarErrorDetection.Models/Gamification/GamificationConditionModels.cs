using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Gamification;

/// <summary>Condición enviada al crear o editar un logro o desafío (se cumplen todas).</summary>
public sealed class GamificationConditionRequest
{
    public Guid ConditionTypeId { get; set; }
    public string ComparisonOperator { get; set; } = ">=";
    public decimal TargetValue { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? ConditionParameters { get; set; }
}

/// <summary>Condición recibida en listados. En medallas y progreso incluye además el avance del estudiante.</summary>
public sealed class GamificationConditionResponse
{
    public Guid Id { get; set; }
    public Guid ConditionId { get; set; }
    public Guid ConditionTypeId { get; set; }
    public string? ConditionCode { get; set; }
    public string? ConditionName { get; set; }
    public string ComparisonOperator { get; set; } = ">=";
    public decimal TargetValue { get; set; }
    public Dictionary<string, object?>? ConditionParameters { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? RequiredValue { get; set; }
    public bool? ConditionMet { get; set; }
    public bool? CanReach { get; set; }
    public decimal? ProgressPercent { get; set; }
}
