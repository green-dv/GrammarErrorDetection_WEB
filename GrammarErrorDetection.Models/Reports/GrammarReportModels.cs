namespace GrammarErrorDetection.Models.Reports;

public sealed class GrammarReportSummary
{
    public Guid? ClassId { get; init; }
    public Guid? UserId { get; init; }
    public long EnrolledStudentCount { get; init; }
    public long StudentsWithAnalyzedTexts { get; init; }
    public decimal? StudentCoveragePercentage { get; init; }
    public long TotalTexts { get; init; }
    public long AnalyzedTexts { get; init; }
    public long TotalSentences { get; init; }
    public long TotalErrors { get; init; }
    public decimal? ErrorsPerTextTotal { get; init; }
    public decimal? ErrorsPerAnalyzedText { get; init; }
    // Oraciones con al menos un error y precisión (% de oraciones sin errores; null si no hay oraciones).
    public long SentencesWithErrors { get; init; }
    public decimal? AccuracyPercentage { get; init; }
}

public sealed class GrammarCategoryReport
{
    public Guid ErrorClassificationId { get; init; }
    public string ErrorClassificationName { get; init; } = "";
    public long ErrorCount { get; init; }
    public long RelatedTextCount { get; init; }
    public long RelatedSentenceCount { get; init; }
    public long AffectedStudentCount { get; init; }
    public long EnrolledStudentCount { get; init; }
    public long StudentsWithAnalyzedTexts { get; init; }
    public decimal? AffectedStudentPercentage { get; init; }
    public decimal? AffectedAnalyzedStudentPercentage { get; init; }
    public decimal? ErrorPercentage { get; init; }
    public decimal? ErrorsPerAnalyzedText { get; init; }
}

public sealed class GrammarReportComparison
{
    public string Scope { get; init; } = "Overall";
    public Guid? ClassId { get; init; }
    public Guid? UserId { get; init; }
    public Guid? ErrorClassificationId { get; init; }
    public string? ErrorClassificationName { get; init; }
    public long CurrentEnrolledStudentCount { get; init; }
    public long PreviousEnrolledStudentCount { get; init; }
    public Guid? PreviousClassId { get; init; }
    public Guid[] PreviousClassIds { get; init; } = [];
    public long StudentsWithPreviousClassCount { get; init; }
    public long StudentsWithoutPreviousClassCount { get; init; }
    public long CurrentTotalTexts { get; init; }
    public long PreviousTotalTexts { get; init; }
    public long CurrentAnalyzedTexts { get; init; }
    public long PreviousAnalyzedTexts { get; init; }
    public long CurrentStudentsWithAnalyzedTexts { get; init; }
    public long PreviousStudentsWithAnalyzedTexts { get; init; }
    public long CurrentErrorCount { get; init; }
    public long PreviousErrorCount { get; init; }
    public long CurrentRelatedTextCount { get; init; }
    public long PreviousRelatedTextCount { get; init; }
    public long CurrentAffectedStudentCount { get; init; }
    public long PreviousAffectedStudentCount { get; init; }
    public decimal? CurrentAffectedStudentPercentage { get; init; }
    public decimal? PreviousAffectedStudentPercentage { get; init; }
    public decimal? CurrentAffectedAnalyzedStudentPercentage { get; init; }
    public decimal? PreviousAffectedAnalyzedStudentPercentage { get; init; }
    public decimal? CurrentErrorsPerAnalyzedText { get; init; }
    public decimal? PreviousErrorsPerAnalyzedText { get; init; }
    public long? ErrorCountChange { get; init; }
    public decimal? ErrorCountChangePercentage { get; init; }
    public long? AffectedStudentCountChange { get; init; }
    public decimal? AffectedStudentPercentagePointChange { get; init; }
    public decimal? AffectedAnalyzedStudentPercentagePointChange { get; init; }
    public decimal? ErrorsPerAnalyzedTextChange { get; init; }
    public decimal? ErrorsPerAnalyzedTextChangePercentage { get; init; }
    public string ComparisonStatus { get; init; } = "NoAnalysisInEitherPeriod";
    public string Trend { get; init; } = "InsufficientData";
}

public sealed record GrammarReportScope(Guid? ClassId = null, Guid? UserId = null);
public sealed record GrammarReportData(GrammarReportSummary Summary, IReadOnlyList<GrammarCategoryReport> Categories, IReadOnlyList<GrammarReportComparison> Comparison);
public sealed record GrammarReportFile(byte[] Content, string FileName, string ContentType);
