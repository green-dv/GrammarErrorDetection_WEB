using System.Globalization;
using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Models.Materials;
using GrammarErrorDetection.Models.Reports;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.WEB.Views.Materials;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace GrammarErrorDetection.WEB.Components.Materials;

/// <summary>
/// Plan de estudio personalizado del estudiante autenticado: material recomendado por sus errores,
/// filtrable por curso (módulo/paralelo/bimestre/año) y por una o varias categorías de error.
/// </summary>
public partial class StudyPlan : IDisposable
{
    /// <summary>Dentro de otra página (Inicio): el título baja a h2.</summary>
    [Parameter] public bool Embedded { get; set; }
    [Inject] private ISupportMaterialService MaterialService { get; set; } = default!;
    [Inject] private IGrammarReportService Reports { get; set; } = default!;
    [Inject] private IRegistrationService Registrations { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    private const int AllCategories = 50;
    private const int MaterialsPerCategory = 5;
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");
    // Paleta de pestañas por categoría (color fuerte, fondo suave).
    private static readonly (string Strong, string Soft)[] Palette =
    [
        ("#2f6fd6", "#e9f1fd"), ("#1f8a5b", "#e6f6ee"), ("#7a4fd6", "#f1ebfd"), ("#d0731f", "#fdf1e6"),
        ("#138c9b", "#e4f5f7"), ("#c2417a", "#fbe9f1"), ("#a77a0f", "#fbf4e2"), ("#4b55c8", "#ecedfb")
    ];

    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? request;
    private Guid viewer;
    private List<SupportMaterialRecommendationResponse> categories = [];
    private GrammarReportData? report;
    private List<StudentCourseResponse> courses = [];
    private readonly HashSet<Guid> selected = [];
    private readonly Dictionary<Guid, int> colorIndex = [];
    private Guid? classId;
    private bool loading, loadingCourses, courseMenuOpen, detailOpen, disposed;
    private string? error, reportError, coursesError;

    private string ApiBaseUrl => Configuration["Api:BaseUrl"]!;
    private int MaterialCount => categories.Sum(category => category.Materials.Count);
    private IEnumerable<SupportMaterialRecommendationResponse> VisibleCategories =>
        selected.Count == 0 ? categories : categories.Where(category => selected.Contains(category.ErrorClassificationId));
    private IEnumerable<(SupportMaterialRecommendationResponse Category, RecommendedMaterialResponse Material)> VisibleCards =>
        VisibleCategories.SelectMany(category => category.Materials.Select(material => (category, material)));
    private StudentCourseResponse? SelectedCourse => courses.FirstOrDefault(course => course.ClassId == classId);
    private IReadOnlyList<GrammarCategoryReport> ReportCategories => report?.Categories.OrderByDescending(row => row.ErrorCount).ToList() ?? [];
    private long MaxCategoryErrors => Math.Max(1, ReportCategories.Select(row => row.ErrorCount).DefaultIfEmpty(0).Max());

    protected override async Task OnInitializedAsync()
    {
        if (Session.Current?.User is not { } user) { Navigation.NavigateTo("/auth/login", replace: true); return; }
        viewer = user.Id;
        var coursesTask = LoadCoursesAsync();
        await LoadAsync();
        await coursesTask;
    }

    private async Task LoadCoursesAsync()
    {
        loadingCourses = true;
        coursesError = null;
        try
        {
            var response = await Registrations.GetMyCoursesAsync(1, 50, lifetime.Token);
            if (response?.Error is { } apiError) throw new InvalidOperationException(apiError.ErrorMessage);
            courses = (response?.Data ?? [])
                .Where(course => course.ClassId != Guid.Empty && (course.Status is null || course.Status.Trim().Equals("Approved", StringComparison.OrdinalIgnoreCase)))
                .GroupBy(course => course.ClassId).Select(group => group.First())
                .OrderByDescending(course => course.Year).ThenByDescending(course => course.Bimester)
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or ArgumentException)
        { coursesError = "No se pudieron cargar tus cursos."; }
        catch (OperationCanceledException) { }
        finally { loadingCourses = false; }
    }

    private async Task LoadAsync()
    {
        if (disposed || viewer == Guid.Empty) return;
        request?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        request = cancellation;
        loading = true;
        error = null;
        reportError = null;
        var scopeClass = classId;
        try
        {
            var recommendationsTask = MaterialService.GetRecommendationsAsync(null, scopeClass, AllCategories, MaterialsPerCategory, cancellation.Token);
            var reportTask = LoadReportAsync(scopeClass, cancellation.Token);
            var response = await recommendationsTask;
            if (disposed || cancellation.IsCancellationRequested) return;
            if (response?.Error is { } apiError)
                throw new InvalidOperationException(apiError.ErrorMessage ?? "No se pudieron obtener las recomendaciones.");
            categories = (response?.Data ?? throw new InvalidOperationException("La respuesta de recomendaciones no tiene el formato esperado."))
                .OrderBy(row => row.Rank)
                .Select(row => { row.Materials = row.Materials.OrderBy(material => material.Rank).ToList(); return row; })
                .Where(row => row.Materials.Count > 0)
                .ToList();
            foreach (var category in categories) Color(category.ErrorClassificationId);
            selected.IntersectWith(categories.Select(category => category.ErrorClassificationId));
            report = await reportTask;
        }
        catch (OperationCanceledException)
        {
            if (!disposed && !lifetime.IsCancellationRequested && ReferenceEquals(request, cancellation))
                error = "La consulta tardó demasiado. Inténtalo de nuevo.";
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or ArgumentException)
        {
            if (!disposed && ReferenceEquals(request, cancellation)) error = ErrorMessage(ex);
        }
        finally
        {
            if (ReferenceEquals(request, cancellation))
            {
                loading = false;
                request = null;
                if (error is not null) { categories = []; report = null; }
            }
        }
    }

    // El detalle es complementario: si falla, la página sigue mostrando las recomendaciones.
    private async Task<GrammarReportData?> LoadReportAsync(Guid? scopeClass, CancellationToken token)
    {
        try { return await Reports.LoadAsync(new GrammarReportScope(scopeClass, viewer), token); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or ArgumentException)
        {
            reportError = "No se pudo cargar el detalle de tus errores.";
            return null;
        }
    }

    private async Task SelectCourseAsync(Guid? id)
    {
        courseMenuOpen = false;
        if (classId == id) return;
        classId = id;
        await LoadAsync();
    }

    private void ToggleCategory(Guid id)
    {
        if (!selected.Add(id)) selected.Remove(id);
    }

    private void ClearCategories() => selected.Clear();

    // ---- Presentación ----

    private (string Strong, string Soft) Color(Guid categoryId)
    {
        if (!colorIndex.TryGetValue(categoryId, out var index))
        {
            index = colorIndex.Count % Palette.Length;
            colorIndex[categoryId] = index;
        }
        return Palette[index];
    }

    private string CategoryStyle(Guid categoryId)
    {
        var (strong, soft) = Color(categoryId);
        return $"--cat:{strong};--cat-soft:{soft}";
    }

    /// <summary>"simple_present" → "Simple present".</summary>
    private static string DisplayName(string name)
    {
        var text = name.Replace('_', ' ').Trim();
        return text.Length == 0 ? "Sin categoría" : char.ToUpper(text[0], Spanish) + text[1..];
    }

    private static string Priority(SupportMaterialRecommendationResponse category) =>
        category.Rank == 1 || category.ErrorPercentage >= 20 ? "alta" : category.ErrorPercentage >= 8 ? "media" : "baja";

    private static string PriorityLabel(string priority) => priority switch
    {
        "alta" => "Alta prioridad", "media" => "Media prioridad", _ => "Baja prioridad"
    };

    private static string CategoryKind(string name)
    {
        var key = name.ToLowerInvariant();
        if (key.Contains("present") || key.Contains("past") || key.Contains("verb") || key.Contains("have_to")) return "time";
        if (key.Contains("prepos")) return "place";
        if (key.Contains("question") || key.StartsWith("how")) return "question";
        if (key.Contains("modal")) return "modal";
        if (key.Contains("compar") || key.Contains("superl")) return "compare";
        return "text";
    }

    private string? DifficultySentence()
    {
        var top = categories.Take(3).Select(category => DisplayName(category.ErrorClassificationName)).ToList();
        return top.Count switch
        {
            0 => null,
            1 => top[0],
            _ => string.Join(", ", top.Take(top.Count - 1)) + " y " + top[^1]
        };
    }

    private decimal? Accuracy => report?.Summary.AccuracyPercentage;

    private string AccuracyLevel => Accuracy switch
    {
        null => "none", >= 80 => "good", >= 50 => "fair", _ => "low"
    };

    private static string CourseDetail(StudentCourseResponse course) =>
        string.Join(" · ", new[]
        {
            string.IsNullOrWhiteSpace(course.Parallel) ? null : $"Paralelo {course.Parallel}",
            course.Bimester > 0 ? $"{course.Bimester}.º bimestre" : null,
            course.Year > 0 ? course.Year.ToString(CultureInfo.InvariantCulture) : null
        }.Where(part => part is not null));

    private static string CourseName(StudentCourseResponse course) =>
        string.IsNullOrWhiteSpace(course.ModuleName) ? "Curso sin nombre" : course.ModuleName;

    private static string Format(RecommendedMaterialResponse material) =>
        MaterialPresentation.IsFileServerLink(material.FileLink)
            ? MaterialPresentation.FormatFromExtension(material.FileExtension, material.MimeType)
            : "URL";

    private static string? ReasonLabel(RecommendedMaterialResponse material) => material.ReasonCode switch
    {
        "EFFECTIVE" => "Ayudó a otros a reducir este error",
        "PEERS_HELPFUL" => "Útil para tus compañeros",
        "FORMAT_PREFERENCE" => "En un formato que te funciona",
        "REVIEW" => "Para repasar",
        "NEW" => "Nuevo para ti",
        _ => null
    };

    private static string Percent(decimal? value) => value is { } number ? number.ToString("0.#", Spanish) + " %" : "—";

    // ---- Interacciones: vistas y "¿Te sirvió?" ----

    private readonly Dictionary<Guid, Task> pendingViews = [];
    private readonly HashSet<Guid> voting = [];
    private MaterialView? viewing;
    private RecommendedMaterialResponse? viewingMaterial;
    private (SupportMaterialRecommendationResponse Category, RecommendedMaterialResponse Material)? lastOpened;
    private string commentDraft = "";
    private bool commentSent;
    private string? feedbackError;

    private void TrackView(SupportMaterialRecommendationResponse category, RecommendedMaterialResponse material, bool downloaded)
    {
        material.Seen = true;
        lastOpened = (category, material);
        pendingViews[material.Id] = RegisterViewAsync(material.Id, downloaded);
    }

    private async Task RegisterViewAsync(Guid materialId, bool downloaded)
    {
        try { await MaterialService.RegisterViewAsync(materialId, downloaded, lifetime.Token); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException or ArgumentException) { }
    }

    private async Task VoteAsync(SupportMaterialRecommendationResponse category, RecommendedMaterialResponse material, bool helpful, string? comment = null)
    {
        if (!voting.Add(material.Id)) return;
        feedbackError = null;
        try
        {
            // El feedback exige una vista previa registrada (p. ej. "Marcar útil" sin haber abierto el enlace).
            if (!material.Seen) TrackView(category, material, false);
            if (pendingViews.TryGetValue(material.Id, out var view)) await view;
            var response = await MaterialService.SetFeedbackAsync(material.Id, helpful, comment, lifetime.Token);
            if (response?.Error is { } apiError) throw new InvalidOperationException(apiError.ErrorMessage);
            material.Helpful = helpful;
            if (comment is not null) commentSent = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or ArgumentException)
        {
            feedbackError = "No se pudo guardar tu respuesta. Inténtalo de nuevo.";
        }
        catch (OperationCanceledException) { }
        finally { voting.Remove(material.Id); }
    }

    private Task SendCommentAsync() =>
        viewingMaterial is { } material && lastOpened is { } last && !string.IsNullOrWhiteSpace(commentDraft)
            ? VoteAsync(last.Category, material, false, commentDraft)
            : Task.CompletedTask;

    private void MarkSeen()
    {
        if (lastOpened is { } last && !last.Material.Seen) TrackView(last.Category, last.Material, false);
    }

    private void Open(SupportMaterialRecommendationResponse category, RecommendedMaterialResponse material)
    {
        var link = MaterialPresentation.ResolveLink(material.FileLink, ApiBaseUrl);
        if (link is null) return;
        TrackView(category, material, false);
        viewingMaterial = material;
        commentDraft = "";
        commentSent = false;
        feedbackError = null;
        viewing = new MaterialView(material.Name, DisplayName(category.ErrorClassificationName), category.ErrorClassificationDescription,
            Format(material), string.IsNullOrWhiteSpace(material.FileTypeName) ? "Recurso" : material.FileTypeName,
            material.FileSize, material.UploadDate,
            string.IsNullOrWhiteSpace(material.UploadedByName) ? null : material.UploadedByName,
            link, material.FileExtension);
    }

    private string? ViewerSubtitle => lastOpened is { } opened
        ? $"Material recomendado para {DisplayName(opened.Category.ErrorClassificationName)}" : null;

    private void Close()
    {
        viewing = null;
        viewingMaterial = null;
    }

    private string ErrorMessage(Exception exception)
    {
        if (exception is HttpRequestException http)
        {
            if (http.StatusCode == HttpStatusCode.Unauthorized)
            {
                Session.Clear();
                Navigation.NavigateTo("/auth/login", replace: true);
                return "Tu sesión venció. Vuelve a iniciar sesión.";
            }
            return http.StatusCode switch
            {
                HttpStatusCode.Forbidden => "No tienes acceso a las recomendaciones de este curso.",
                HttpStatusCode.BadRequest => "No se pudieron calcular las recomendaciones para el curso seleccionado.",
                HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed => "La API en ejecución todavía no ofrece recomendaciones de material.",
                _ => "No se pudo consultar tu plan de estudio. Inténtalo de nuevo."
            };
        }
        return exception switch
        {
            JsonException => "La respuesta no tiene el formato esperado.",
            // Mensajes de la API o propios (en español); las guardas de argumentos traen textos técnicos.
            InvalidOperationException when !string.IsNullOrWhiteSpace(exception.Message) => exception.Message,
            _ => "No se pudo preparar la consulta. Recarga la página e inténtalo de nuevo."
        };
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        request?.Cancel();
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
