namespace GrammarErrorDetection.Models.Texts;

public sealed record TextUploadRequest(string OriginalContent);

public sealed class TextSearchRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 5;
    public Guid? Id { get; set; }
    public Guid? UserId { get; set; }
    public string? OriginalContent { get; set; }
}

public sealed class TextResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string OriginalContent { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; }
    public int ErrorCount { get; set; }
    public List<TextSentenceResponse> Sentences { get; set; } = [];
}

public sealed class TextSentenceResponse
{
    public Guid Id { get; set; }
    public Guid TextId { get; set; }
    public int SentenceIndex { get; set; }
    public string OriginalSentence { get; set; } = "";
    public string? CorrectedSentence { get; set; }
    public int ErrorCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; }
    public List<TextErrorResponse> Errors { get; set; } = [];
}

public sealed class TextErrorResponse
{
    public Guid Id { get; set; }
    public Guid SentenceId { get; set; }
    public Guid ErrorClassificationId { get; set; }
    public string? ErrorClassificationName { get; set; }
    public string? ErrorToken { get; set; }
    public int? ErrorTokenStartIndex { get; set; }
    public int? ErrorTokenEndIndex { get; set; }
    public string? SuggestedCorrection { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; }
}
