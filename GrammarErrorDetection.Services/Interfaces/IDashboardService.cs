using GrammarErrorDetection.Models.Dashboards;

namespace GrammarErrorDetection.Services.Interfaces;

/// <summary>Dashboard del estudiante autenticado: la API toma el usuario del token.</summary>
public interface IDashboardService
{
    Task<StudentDashboardResponse> GetStudentAsync(Guid? classId = null, CancellationToken cancellationToken = default);
    Task<DashboardPage<FrequentErrorItem>> GetStudentFrequentErrorsAsync(int pageNumber, int pageSize, Guid? classId = null, CancellationToken cancellationToken = default);
    Task<StudentErrorsResponse> GetStudentErrorsAsync(Guid? classId, CancellationToken cancellationToken = default);
    Task<DashboardPage<TextHistoryItem>> GetStudentTextsAsync(int pageNumber, int pageSize, Guid? classId = null, CancellationToken cancellationToken = default);
    Task<DashboardPage<ChallengeProgressItem>> GetStudentChallengesAsync(int pageNumber, int pageSize, bool onlyPending = false, CancellationToken cancellationToken = default);
    Task<DashboardPage<PointHistoryItem>> GetStudentPointsAsync(int pageNumber, int pageSize, string? source = null, CancellationToken cancellationToken = default);

    /// <summary>Docente autenticado: sus cursos o uno de ellos.</summary>
    Task<TeacherDashboardResponse> GetTeacherAsync(Guid? classId, CancellationToken cancellationToken = default);
    /// <summary>Docente: siempre sus cursos. Coordinador: teacherId opcional.</summary>
    Task<DashboardPage<AtRiskStudent>> GetAtRiskAsync(int pageNumber, int pageSize, Guid? classId = null, string? reason = null,
        Guid? teacherId = null, CancellationToken cancellationToken = default, int? year = null, int? bimester = null);
    Task<CoordinatorDashboardResponse> GetCoordinatorAsync(int? year, int? bimester, CancellationToken cancellationToken = default);
    /// <summary>Coordinador: docentes por promedio de nota final de sus estudiantes. order: Best o Worst.</summary>
    Task<DashboardPage<TeacherRankingItem>> GetTeacherRankingAsync(int pageNumber, int pageSize, int? year = null, int? bimester = null,
        string order = "Best", CancellationToken cancellationToken = default);
    Task<DashboardPage<ClassCoverageItem>> GetClassCoverageAsync(int pageNumber, int pageSize, int? year = null, int? bimester = null,
        CancellationToken cancellationToken = default);
}
