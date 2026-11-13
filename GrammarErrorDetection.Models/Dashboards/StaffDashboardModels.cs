namespace GrammarErrorDetection.Models.Dashboards;

public sealed class TeacherDashboardResponse
{
    public TeacherDashboardKpis? Kpis { get; set; }
    public List<TeacherCourseSummary> Courses { get; set; } = [];
    public List<StaffErrorCategory> TopCategories { get; set; } = [];
    public List<MaterialUsageItem> MaterialUsage { get; set; } = [];
    public List<TeacherWeeklyActivity> WeeklyActivity { get; set; } = [];
    public List<ModulePerformanceItem> ModulePerformance { get; set; } = [];
}

public sealed class ModulePerformanceItem
{
    public Guid ModuleNameId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int ModuleLevel { get; set; }
    public long CourseCount { get; set; }
    public long EnrolledStudents { get; set; }
    public long GradedStudents { get; set; }
    public decimal? Exam1Average { get; set; }
    public decimal? Exam2Average { get; set; }
    public decimal? FinalAverage { get; set; }
    public decimal? PassRatePercentage { get; set; }
    public long AnalyzedTexts { get; set; }
    public long TotalErrors { get; set; }
    public decimal? ErrorsPerText { get; set; }
    public decimal? AccuracyPercentage { get; set; }
    public string? TopErrorClassificationName { get; set; }
}

public sealed class TeacherRankingItem
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public long CourseCount { get; set; }
    public long EnrolledStudents { get; set; }
    public long GradedStudents { get; set; }
    public decimal? FinalAverage { get; set; }
    public decimal? PassRatePercentage { get; set; }
    public long AnalyzedTexts { get; set; }
    public long TotalErrors { get; set; }
    public decimal? ErrorsPerText { get; set; }
    public decimal? AccuracyPercentage { get; set; }
    public long AtRiskStudents { get; set; }
    public long Rank { get; set; }
}

public sealed class TeacherWeeklyActivity
{
    public DateTime WeekStart { get; set; }
    public long AnalyzedTexts { get; set; }
    public long ActiveStudents { get; set; }
    public decimal? AccuracyPercentage { get; set; }
    public decimal? ErrorsPerText { get; set; }
}

public sealed class TeacherDashboardKpis
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public long AtRiskNoTexts { get; set; }
    public long AtRiskLowScore { get; set; }
    public long TextsLast30 { get; set; }
    public long TextsPrevious30 { get; set; }
    public decimal? AccuracyLast30 { get; set; }
    public decimal? AccuracyPrevious30 { get; set; }
    public int CourseCount { get; set; }
    public long EnrolledStudents { get; set; }
    public long StudentsWithTexts { get; set; }
    public decimal? CoveragePercentage { get; set; }
    public long AnalyzedTexts { get; set; }
    public decimal? AccuracyPercentage { get; set; }
    public decimal? ErrorsPerText { get; set; }
    public decimal? AverageFinalScore { get; set; }
    public decimal? PassRatePercentage { get; set; }
    public long GradedStudents { get; set; }
    public long AtRiskStudents { get; set; }
}

public sealed class TeacherCourseSummary
{
    public Guid ClassId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int ModuleLevel { get; set; }
    public int Year { get; set; }
    public int Bimester { get; set; }
    public string? Parallel { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public long EnrolledStudents { get; set; }
    public long StudentsWithTexts { get; set; }
    public decimal? CoveragePercentage { get; set; }
    public long AnalyzedTexts { get; set; }
    public decimal? AccuracyPercentage { get; set; }
    public decimal? ErrorsPerText { get; set; }
    public decimal? Exam1Average { get; set; }
    public decimal? Exam2Average { get; set; }
    public decimal? FinalAverage { get; set; }
    public decimal? PassRatePercentage { get; set; }
    public long AtRiskStudents { get; set; }
}

public sealed class StaffErrorCategory
{
    public Guid ErrorClassificationId { get; set; }
    public string ErrorClassificationName { get; set; } = string.Empty;
    public long ErrorCount { get; set; }
    public decimal? ErrorPercentage { get; set; }
    public long AffectedStudents { get; set; }
    public decimal? AffectedStudentPercentage { get; set; }
}

public sealed class MaterialUsageItem
{
    public Guid SupportMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ErrorClassificationName { get; set; } = string.Empty;
    public long Viewers { get; set; }
    public long Views { get; set; }
    public long HelpfulVotes { get; set; }
    public long NotHelpfulVotes { get; set; }
}

public sealed class AtRiskStudent
{
    public Guid UserId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Guid ClassId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Bimester { get; set; }
    public string? Parallel { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? Exam1Score { get; set; }
    public decimal? Exam2Score { get; set; }
    public decimal? FinalScore { get; set; }
    public long AnalyzedTexts { get; set; }
    public DateTime? LastTextAt { get; set; }
    public decimal? AccuracyPercentage { get; set; }
}

public sealed class CoordinatorDashboardResponse
{
    public CoordinatorDashboardKpis? Kpis { get; set; }
    public CoordinatorGamificationSummary? Gamification { get; set; }
    public List<ModuleErrorCategory> ErrorsByModule { get; set; } = [];
    public List<InstitutionErrorCategory> TopCategories { get; set; } = [];
    public List<ModulePerformanceItem> ModulePerformance { get; set; } = [];
}

public sealed class InstitutionErrorCategory
{
    public Guid ErrorClassificationId { get; set; }
    public string ErrorClassificationName { get; set; } = string.Empty;
    public long ErrorCount { get; set; }
    public decimal? ErrorPercentage { get; set; }
    public long ModuleCount { get; set; }
}

public sealed class CoordinatorDashboardKpis
{
    public long ActiveStudents { get; set; }
    public long ActiveTeachers { get; set; }
    public long ActiveCoordinators { get; set; }
    public long PendingRegistrations { get; set; }
    public int CourseCount { get; set; }
    public long EnrolledStudents { get; set; }
    public long StudentsWithTexts { get; set; }
    public decimal? CoveragePercentage { get; set; }
    public long AnalyzedTexts { get; set; }
    public decimal? AccuracyPercentage { get; set; }
    public decimal? ErrorsPerText { get; set; }
    public decimal? AverageFinalScore { get; set; }
    public decimal? PassRatePercentage { get; set; }
    public long AtRiskStudents { get; set; }
    public long ClassesWithActivity { get; set; }
    public long AtRiskNoTexts { get; set; }
    public long AtRiskLowScore { get; set; }
    public decimal? AtRiskPercentage { get; set; }
    public int? PreviousCourseCount { get; set; }
    public long? PreviousEnrolledStudents { get; set; }
    public long? PreviousAnalyzedTexts { get; set; }
    public decimal? PreviousAccuracyPercentage { get; set; }
    public decimal? PreviousAtRiskPercentage { get; set; }
    public decimal? PreviousAverageFinalScore { get; set; }
    public decimal? PreviousPassRatePercentage { get; set; }
    public decimal? PreviousErrorsPerText { get; set; }
    public long GradedStudents { get; set; }
}

public sealed class CoordinatorGamificationSummary
{
    public long AchievementsAwarded { get; set; }
    public long PointsAwarded { get; set; }
    public long StudentsWithPoints { get; set; }
    public long ChallengesCompleted { get; set; }
    public long ActiveChallenges { get; set; }
    public long ActiveAchievements { get; set; }
    public long? PreviousAchievementsAwarded { get; set; }
    public long? PreviousPointsAwarded { get; set; }
    public long? PreviousChallengesCompleted { get; set; }
}

public sealed class ModuleErrorCategory
{
    public Guid ModuleNameId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int ModuleLevel { get; set; }
    public Guid ErrorClassificationId { get; set; }
    public string ErrorClassificationName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public long ErrorCount { get; set; }
    public long ModuleErrorCount { get; set; }
    public decimal? ErrorPercentage { get; set; }
}

public sealed class ClassCoverageItem
{
    public Guid ClassId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int ModuleLevel { get; set; }
    public int Year { get; set; }
    public int Bimester { get; set; }
    public string? Parallel { get; set; }
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public long EnrolledStudents { get; set; }
    public long StudentsWithTexts { get; set; }
    public decimal? CoveragePercentage { get; set; }
    public long AnalyzedTexts { get; set; }
    public decimal? AccuracyPercentage { get; set; }
    public decimal? ErrorsPerText { get; set; }
    public decimal? AverageFinalScore { get; set; }
    public long AtRiskStudents { get; set; }
}
