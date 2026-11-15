namespace GrammarErrorDetection.Models.Evaluations;

public class EvaluationWriteRequest
{
    public Guid RegistrationId { get; set; }
    public int? Period { get; set; }
    public int? WritingScore { get; set; }
    public int? GrammarScore { get; set; }
    public int? VocabularyScore { get; set; }
    public int? ReadingScore { get; set; }
    public int? ListeningScore { get; set; }
    public int? OralScore { get; set; }
    public int? AcademicPerformance { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class EvaluationUpdateRequest : EvaluationWriteRequest
{
    public Guid Id { get; set; }
}

public sealed class EvaluationResponse : EvaluationWriteRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
