using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Dashboards;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Dashboards;

public sealed class DashboardService(IApiClient apiClient) : IDashboardService
{
    public static readonly string[] PointSources = ["Text", "Evaluation", "Achievement", "Challenge", "Course", "Other"];

    public async Task<StudentDashboardResponse> GetStudentAsync(Guid? classId = null, CancellationToken cancellationToken = default)
    {
        if (classId == Guid.Empty) throw new ArgumentException("El curso no es válido.", nameof(classId));
        var path = "api/Dashboard/student" + (classId is { } id ? $"?ClassId={id:D}" : "");
        return Require(await apiClient.GetAsync<ApiResponse<StudentDashboardResponse>>(path, cancellationToken), "No se pudo cargar tu resumen.");
    }

    public Task<DashboardPage<FrequentErrorItem>> GetStudentFrequentErrorsAsync(int pageNumber, int pageSize, Guid? classId = null,
        CancellationToken cancellationToken = default)
    {
        if (classId == Guid.Empty) throw new ArgumentException("El curso no es válido.", nameof(classId));
        return Page<FrequentErrorItem>("api/Dashboard/student/frequent-errors", pageNumber, pageSize,
            classId is { } id ? $"&ClassId={id:D}" : "", "No se pudieron cargar tus errores frecuentes.", cancellationToken);
    }

    public async Task<StudentErrorsResponse> GetStudentErrorsAsync(Guid? classId, CancellationToken cancellationToken = default)
    {
        if (classId == Guid.Empty) throw new ArgumentException("El curso no es válido.", nameof(classId));
        var path = "api/Dashboard/student/errors" + (classId is { } id ? $"?ClassId={id:D}" : "");
        return Require(await apiClient.GetAsync<ApiResponse<StudentErrorsResponse>>(path, cancellationToken),
            "No se pudieron cargar los errores del curso.");
    }

    public Task<DashboardPage<TextHistoryItem>> GetStudentTextsAsync(int pageNumber, int pageSize, Guid? classId = null,
        CancellationToken cancellationToken = default)
    {
        if (classId == Guid.Empty) throw new ArgumentException("El curso no es válido.", nameof(classId));
        return Page<TextHistoryItem>("api/Dashboard/student/texts", pageNumber, pageSize,
            classId is { } id ? $"&ClassId={id:D}" : "", "No se pudo cargar tu historial de textos.", cancellationToken);
    }

    public Task<DashboardPage<ChallengeProgressItem>> GetStudentChallengesAsync(int pageNumber, int pageSize, bool onlyPending = false,
        CancellationToken cancellationToken = default)
        => Page<ChallengeProgressItem>("api/Dashboard/student/challenges", pageNumber, pageSize,
            onlyPending ? "&OnlyPending=true" : "", "No se pudieron cargar los desafíos.", cancellationToken);

    public Task<DashboardPage<PointHistoryItem>> GetStudentPointsAsync(int pageNumber, int pageSize, string? source = null,
        CancellationToken cancellationToken = default)
    {
        if (source is not null && !PointSources.Contains(source, StringComparer.Ordinal))
            throw new ArgumentException("El origen de puntos no es válido.", nameof(source));
        return Page<PointHistoryItem>("api/Dashboard/student/points", pageNumber, pageSize,
            source is null ? "" : $"&Source={Uri.EscapeDataString(source)}", "No se pudo cargar tu historial de puntos.", cancellationToken);
    }

    public async Task<TeacherDashboardResponse> GetTeacherAsync(Guid? classId, CancellationToken cancellationToken = default)
    {
        if (classId == Guid.Empty) throw new ArgumentException("El curso no es válido.", nameof(classId));
        var path = "api/Dashboard/teacher" + (classId is { } id ? $"?ClassId={id:D}" : "");
        return Require(await apiClient.GetAsync<ApiResponse<TeacherDashboardResponse>>(path, cancellationToken),
            "No se pudo cargar el resumen de tus cursos.");
    }

    public Task<DashboardPage<AtRiskStudent>> GetAtRiskAsync(int pageNumber, int pageSize, Guid? classId = null, string? reason = null,
        Guid? teacherId = null, CancellationToken cancellationToken = default, int? year = null, int? bimester = null)
    {
        if (classId == Guid.Empty || teacherId == Guid.Empty) throw new ArgumentException("El curso o docente no es válido.");
        if (reason is not null && reason is not ("NoTexts" or "LowScore")) throw new ArgumentException("El motivo no es válido.", nameof(reason));
        var filters = (classId is { } c ? $"&ClassId={c:D}" : "") + (reason is null ? "" : $"&Reason={reason}")
            + (teacherId is { } t ? $"&TeacherId={t:D}" : "") + Period(year, bimester);
        return Page<AtRiskStudent>("api/Dashboard/teacher/at-risk", pageNumber, pageSize, filters,
            "No se pudo cargar la lista de estudiantes en riesgo.", cancellationToken);
    }

    public async Task<CoordinatorDashboardResponse> GetCoordinatorAsync(int? year, int? bimester, CancellationToken cancellationToken = default)
    {
        var query = Period(year, bimester);
        return Require(await apiClient.GetAsync<ApiResponse<CoordinatorDashboardResponse>>(
            "api/Dashboard/coordinator" + (query.Length > 0 ? "?" + query[1..] : ""), cancellationToken), "No se pudo cargar el resumen institucional.");
    }

    public Task<DashboardPage<ClassCoverageItem>> GetClassCoverageAsync(int pageNumber, int pageSize, int? year = null, int? bimester = null,
        CancellationToken cancellationToken = default)
        => Page<ClassCoverageItem>("api/Dashboard/coordinator/classes", pageNumber, pageSize, Period(year, bimester),
            "No se pudo cargar la cobertura por curso.", cancellationToken);

    public Task<DashboardPage<TeacherRankingItem>> GetTeacherRankingAsync(int pageNumber, int pageSize, int? year = null, int? bimester = null,
        string order = "Best", CancellationToken cancellationToken = default)
    {
        if (order is not ("Best" or "Worst")) throw new ArgumentException("El orden no es válido.", nameof(order));
        return Page<TeacherRankingItem>("api/Dashboard/coordinator/teachers", pageNumber, pageSize, Period(year, bimester) + $"&Order={order}",
            "No se pudo cargar el rendimiento de los docentes.", cancellationToken);
    }

    private static string Period(int? year, int? bimester)
    {
        if (year is < 1) throw new ArgumentOutOfRangeException(nameof(year));
        if (bimester is < 1 or > 6 || (bimester is not null && year is null)) throw new ArgumentOutOfRangeException(nameof(bimester));
        return (year is { } y ? $"&Year={y.ToString(CultureInfo.InvariantCulture)}" : "")
            + (bimester is { } b ? $"&Bimester={b.ToString(CultureInfo.InvariantCulture)}" : "");
    }

    private async Task<DashboardPage<T>> Page<T>(string path, int pageNumber, int pageSize, string filters, string error,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);
        var response = await apiClient.GetAsync<ApiResponse<List<T>>>(
            $"{path}?PageNumber={pageNumber.ToString(CultureInfo.InvariantCulture)}&PageSize={pageSize.ToString(CultureInfo.InvariantCulture)}{filters}",
            cancellationToken);
        List<T> items = Require(response, error);
        return new DashboardPage<T>(items, response!.TotalCount ?? items.Count);
    }

    private static T Require<T>(ApiResponse<T>? response, string error) where T : class
        => response?.Data is { } data && response.Error is not { ErrorCode: not 0 } ? data : throw new InvalidOperationException(error);
}
