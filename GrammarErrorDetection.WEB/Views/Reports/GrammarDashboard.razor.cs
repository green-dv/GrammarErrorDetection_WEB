using System.Globalization;
using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Reports;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GrammarErrorDetection.WEB.Views.Reports;

public partial class GrammarDashboard : IAsyncDisposable
{
    [Parameter] public Guid? UserId { get; set; }
    [Parameter] public Guid? ClassId { get; set; }
    [Parameter] public string? CourseName { get; set; }
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private IGrammarReportService Reports { get; set; } = default!;
    [Inject] private ICourseService Courses { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");
    private static readonly string[] Colors = ["#f27672", "#a486ec", "#22b5ad", "#efb54d", "#4e9fe6"];
    private Guid selectedClass;
    private Guid? previousClass, previousUser;
    private Guid viewer;
    private string roleSignature = "";
    private bool initialized, loading, loadingCourses, exporting, disposed;
    private int version;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? requestCancellation, exportCancellation;
    private IJSObjectReference? downloadModule;
    private List<ReportCourseOption> courseOptions = [];
    private string category = "";
    private string? error, exportError;
    private GrammarReportData? data;
    private bool IsPersonal => UserId.HasValue;
    private GrammarReportScope Scope => IsPersonal ? new(UserId: UserId) : new(ClassId: selectedClass);
    private string ReportTitle => IsPersonal ? UserId == viewer ? "Mi desempeño gramatical" : "Desempeño gramatical del usuario" : "Desempeño gramatical del curso";
    private bool Allowed => viewer != Guid.Empty && Session.Current?.User?.Id == viewer && UserId != Guid.Empty && ClassId != Guid.Empty &&
        (Session.IsInRole("Student") || Session.IsInRole("Teacher") || Session.IsInRole("Coordinator")) &&
        (IsPersonal ? !ClassId.HasValue && (UserId == viewer || Session.IsInRole("Coordinator")) : Session.IsInRole("Teacher") || Session.IsInRole("Coordinator"));
    private string SelectedCourseName => courseOptions.FirstOrDefault(course => course.ClassId == selectedClass)?.Name ?? CourseName ?? "Curso seleccionado";
    private ReportCourseOption? SelectedCourse => courseOptions.FirstOrDefault(course => course.ClassId == selectedClass);
    private string CurrentLabel => IsPersonal ? "Todos los cursos y prácticas personales" : "Curso actual";
    private string PreviousLabel => Overall.PreviousClassIds.Length > 1 ? $"Historial de {Overall.PreviousClassIds.Length} cursos anteriores" : Overall.PreviousClassIds.Length == 1 || Overall.PreviousClassId.HasValue ? "Curso anterior" : Overall.StudentsWithPreviousClassCount > 0 ? "Historial anterior" : "Sin curso anterior registrado";
    private GrammarReportComparison Overall => data!.Comparison.Single(row => row.Scope == "Overall");
    private IEnumerable<GrammarCategoryReport> FilteredCategories => data!.Categories.Where(row => string.IsNullOrEmpty(category) || row.ErrorClassificationId.ToString() == category);
    private IEnumerable<GrammarReportComparison> FilteredComparison => data!.Comparison.Where(row => row.Scope == "Category" && (string.IsNullOrEmpty(category) || row.ErrorClassificationId.ToString() == category)).OrderByDescending(row => row.CurrentErrorCount).ThenBy(row => row.ErrorClassificationId);
    private List<GrammarCategoryReport> ChartCategories => data!.Categories.OrderBy(row => row.ErrorClassificationId).ToList();
    private IEnumerable<CategoryChoice> CategoryOptions => data is null ? [] : data.Categories.Select(row => new CategoryChoice(row.ErrorClassificationId, row.ErrorClassificationName))
        .Concat(data.Comparison.Where(row => row.Scope == "Category").Select(row => new CategoryChoice(row.ErrorClassificationId!.Value, row.ErrorClassificationName ?? "Categoría sin nombre")))
        .GroupBy(row => row.Id).Select(group => group.First()).OrderBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase);
    private long MaxErrors => Math.Max(10, (long)Math.Ceiling((data?.Categories.Select(row => row.ErrorCount).DefaultIfEmpty(0).Max() ?? 0) / 10m) * 10);
    private string RadarDescription => "Errores por categoría. " + string.Join(". ", data!.Categories.Select(row => $"{row.ErrorClassificationName}: {row.ErrorCount}"));
    private string RadarValuePoints => string.Join(" ", ChartCategories.Select((row, index) =>
    {
        var point = RadarPoint(index, (double)row.ErrorCount / MaxErrors); return $"{Svg(point.X)},{Svg(point.Y)}";
    }));

    protected override void OnInitialized() { viewer = Session.Current?.User?.Id ?? Guid.Empty; roleSignature = string.Join(",", Session.Roles.Order()); Session.Changed += SessionChanged; }
    protected override async Task OnParametersSetAsync()
    {
        if (initialized && previousUser == UserId && previousClass == ClassId) return;
        initialized = true; previousUser = UserId; previousClass = ClassId; selectedClass = ClassId ?? Guid.Empty; category = "";
        await InitializeAsync();
    }
    private void SessionChanged() => _ = InvokeAsync(async () =>
    {
        var roles = string.Join(",", Session.Roles.Order());
        if (!Allowed || roleSignature != roles)
        {
            roleSignature = roles; version++; requestCancellation?.Cancel(); exportCancellation?.Cancel(); data = null; courseOptions = []; loading = loadingCourses = false;
            if (Allowed) await InitializeAsync();
        }
        StateHasChanged();
    });
    private bool IsCurrent(int current) => !disposed && current == version && Allowed;
    private async Task InitializeAsync()
    {
        requestCancellation?.Cancel(); exportCancellation?.Cancel(); var current = ++version;
        data = null; error = exportError = null; courseOptions = []; loading = loadingCourses = false;
        if (!Allowed) return;
        if (IsPersonal) { await LoadReportAsync(); return; }
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        requestCancellation = cancellation; loadingCourses = true;
        try
        {
            var options = await ReadCoursesAsync(cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!IsCurrent(current)) return;
            courseOptions = options;
            selectedClass = ClassId ?? options.FirstOrDefault()?.ClassId ?? Guid.Empty;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or OperationCanceledException)
        { if (IsCurrent(current) && !cancellation.IsCancellationRequested) HandleError(ex, false); }
        finally { if (IsCurrent(current)) loadingCourses = false; if (ReferenceEquals(requestCancellation, cancellation)) requestCancellation = null; }
        if (IsCurrent(current) && error is null && selectedClass != Guid.Empty) await LoadReportAsync();
    }
    private async Task<List<ReportCourseOption>> ReadCoursesAsync(CancellationToken token)
    {
        var found = new Dictionary<Guid, ReportCourseOption>();
        for (var number = 1; ; number++)
        {
            var added = 0;
            var rows = Require(await Courses.SearchAsync(new() { PageNumber = number, PageSize = 100, Filter = new() { Id = ClassId?.ToString("D"), TeacherId = Session.IsInRole("Teacher") ? viewer.ToString("D") : null } }, token));
            if (rows.Any(row => row.Id == Guid.Empty || (ClassId.HasValue && row.Id != ClassId) || Session.IsInRole("Teacher") && row.TeacherId != viewer))
                throw new InvalidOperationException("El listado recibido no corresponde al curso o docente solicitado.");
            foreach (var row in rows) if (found.TryAdd(row.Id, new(row.Id, row.ModuleName ?? "Módulo sin nombre", row.Year, row.Bimester, row.Parallel))) added++;
            token.ThrowIfCancellationRequested();
            if (rows.Count < 100) break;
            if (added == 0) throw new InvalidOperationException("El listado de cursos no avanzó de página.");
        }
        if (ClassId.HasValue && !found.ContainsKey(ClassId.Value)) throw new InvalidOperationException("El curso no está disponible para esta cuenta.");
        return found.Values.OrderByDescending(row => row.Year).ThenByDescending(row => row.Bimester).ThenBy(row => row.Name).ToList();
    }
    private async Task LoadReportAsync()
    {
        requestCancellation?.Cancel(); exportCancellation?.Cancel(); var current = ++version;
        data = null; error = exportError = null; category = "";
        if (!Allowed || (!IsPersonal && selectedClass == Guid.Empty)) return;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        requestCancellation = cancellation; loading = true;
        var scope = Scope;
        try
        {
            var result = await Reports.LoadAsync(scope, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!IsCurrent(current)) return;
            data = result;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or OperationCanceledException)
        { if (IsCurrent(current) && !cancellation.IsCancellationRequested) HandleError(ex, false); }
        finally { if (current == version) loading = false; if (ReferenceEquals(requestCancellation, cancellation)) requestCancellation = null; }
    }
    private Task RetryAsync() => !IsPersonal && courseOptions.Count == 0 ? InitializeAsync() : LoadReportAsync();
    private async Task ChangeCourse(ChangeEventArgs args)
    {
        if (ClassId.HasValue || !Guid.TryParse(args.Value?.ToString(), out var id) || !courseOptions.Any(row => row.ClassId == id)) return;
        selectedClass = id; await LoadReportAsync();
    }
    private async Task ExportAsync()
    {
        if (!Allowed || data is null || loading || exporting) return;
        var current = version; var scope = Scope;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        exportCancellation = cancellation; exporting = true; exportError = null;
        try
        {
            var file = await Reports.ExportAsync(scope, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!IsCurrent(current)) return;
            downloadModule ??= await JS.InvokeAsync<IJSObjectReference>("import", cancellation.Token, "./js/grammar-report-download.js");
            if (!IsCurrent(current)) return;
            using var stream = new MemoryStream(file.Content);
            using var reference = new DotNetStreamReference(stream);
            await downloadModule.InvokeVoidAsync("downloadReport", cancellation.Token, file.FileName, file.ContentType, reference);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or OperationCanceledException or JSException)
        { if (IsCurrent(current) && !cancellation.IsCancellationRequested) HandleError(ex, true); }
        finally { exporting = false; if (ReferenceEquals(exportCancellation, cancellation)) exportCancellation = null; }
    }
    private void HandleError(Exception ex, bool download)
    {
        if (ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized }) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); return; }
        var message = ex switch
        {
            HttpRequestException { StatusCode: HttpStatusCode.Forbidden } => "No tienes acceso al curso o estudiante solicitado.",
            HttpRequestException http => http.Message,
            OperationCanceledException => "La consulta tardó demasiado. Inténtalo de nuevo.",
            JsonException => "El servicio devolvió una respuesta que no se pudo interpretar.",
            JSException => "No se pudo iniciar la descarga. Inténtalo de nuevo.",
            _ => ex.Message
        };
        if (download) exportError = message; else { error = message; data = null; }
    }
    private static T Require<T>(ApiResponse<T>? response) where T : class => response?.Data is not null && response.Error is not { ErrorCode: not 0 } ? response.Data : throw new InvalidOperationException(response?.Error?.ErrorMessage ?? "No se pudo consultar el listado de cursos.");
    private void SelectCategory(Guid id) => category = category == id.ToString() ? "" : id.ToString();
    private void ClearCategory() => category = "";
    private static string Number(decimal? value, string format = "0.00") => value?.ToString(format, Spanish) ?? "—";
    private static string Signed(decimal? value) => value.HasValue ? value.Value.ToString("+0.##;-0.##;0", Spanish) : "Sin comparación";
    private static string ChangeWithUnit(decimal? value, string unit, bool fractional = false) => value.HasValue ? $"{(fractional ? value.Value.ToString("+0.00;-0.00;0.00", Spanish) : Signed(value))} {unit}" : "Sin comparación";
    private static string RelativeChange(decimal? value) => value.HasValue ? $"{value.Value.ToString("+0.00;-0.00;0.00", Spanish)} %" : "Sin base porcentual";
    private static string TrendLabel(string trend) => trend switch { "Improved" => "Mejora", "Worsened" => "Aumenta la dificultad", "Unchanged" => "Sin cambios", _ => "Sin datos comparables" };
    private static string TrendClass(string trend) => trend switch { "Improved" => "improved", "Worsened" => "worsened", "Unchanged" => "unchanged", _ => "insufficient" };
    private string ComparisonDescription(GrammarReportComparison row) => row.ComparisonStatus switch
    {
        "NoCurrentAnalysis" => "No hay textos analizados en el curso actual.",
        "NoPreviousAnalysis" => "No hay textos analizados en el historial anterior.",
        "NoAnalysisInEitherPeriod" => "No hay análisis disponibles en el curso actual ni en el historial anterior.",
        _ => $"{Number(row.PreviousErrorsPerAnalyzedText)} antes y {Number(row.CurrentErrorsPerAnalyzedText)} ahora, por párrafo analizado."
    };
    private string Color(Guid id) => Colors[Math.Max(0, ChartCategories.FindIndex(row => row.ErrorClassificationId == id)) % Colors.Length];
    private static string Percent(long value, long maximum) => (100m * value / maximum).ToString("0.##", CultureInfo.InvariantCulture);
    private static string Svg(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private (double X, double Y) RadarPoint(int index, double scale) { var angle = -Math.PI / 2 + index * 2 * Math.PI / Math.Max(1, ChartCategories.Count); return (200 + 90 * scale * Math.Cos(angle), 142 + 90 * scale * Math.Sin(angle)); }
    private string RadarPoints(double scale) => string.Join(" ", Enumerable.Range(0, ChartCategories.Count).Select(index => { var point = RadarPoint(index, scale); return $"{Svg(point.X)},{Svg(point.Y)}"; }));
    public async ValueTask DisposeAsync()
    {
        disposed = true; version++; Session.Changed -= SessionChanged;
        lifetime.Cancel(); lifetime.Dispose();
        if (downloadModule is not null) { try { await downloadModule.DisposeAsync(); } catch (JSDisconnectedException) { } }
    }
    private sealed record CategoryChoice(Guid Id, string Name);
    private sealed record ReportCourseOption(Guid ClassId, string Name, int Year, int Bimester, string? Parallel)
    {
        public string Label => $"{Name} · {Year} / Bimestre {Bimester} · {Parallel ?? "—"}";
    }
}
