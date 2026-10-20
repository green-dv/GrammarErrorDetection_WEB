using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Views.Courses;

public partial class CourseList : IDisposable
{
    [Inject] private ICourseService CourseService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private CourseSearchFilter filters = new();
    private CourseSearchFilter? appliedFilter;
    private List<CourseSearchResponse> courses = [];
    [Inject] private IUserService UserService { get; set; } = default!;
    private List<UserLookupOption> modules = [];
    private List<UserSearchResponse> teachers = [];
    private List<ClassroomLookupOption> classrooms = [];
    private static readonly Guid TeacherRoleId = Guid.Parse("f2700000-0000-4000-8000-000000000002");
    private int page = 1, pageSize = 20, loadedPageSize = 20, requestVersion;
    private bool busy, loadingCatalogs, hasNext, disposed, redirected;
    private string? error, catalogError, status;
    private List<string> validationErrors = [];
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchCancellation;
    private bool IsTeacher => Session.IsInRole("Teacher");
    private bool CanCreate => Session.IsInRole("Coordinator") && !IsTeacher;
    private bool showCreate;
    private bool showGenerate;
    private void OpenGenerate() { if (CanCreate && !busy && CheckSession()) showGenerate = true; }
    private void CloseGenerate() => showGenerate = false;
    private async Task CoursesGeneratedAsync(CourseGenerationResponse response)
    {
        filters = new CourseSearchFilter
        {
            Year = response.SourceBimester == 6 ? response.SourceYear + 1 : response.SourceYear,
            Bimester = response.SourceBimester == 6 ? 1 : response.SourceBimester + 1
        };
        appliedFilter = filters.Normalize();
        validationErrors.Clear();
        await LoadAsync(1);
        if (!disposed) status = error is null
            ? $"Generación completada. {response.CreatedClassIds?.Count ?? 0} cursos creados y {response.CreatedRegistrationIds?.Count ?? 0} inscripciones creadas."
            : "La generación se completó. No se pudo actualizar el listado; pulsa Reintentar para consultarlo.";
    }
    private CourseSearchResponse? editingCourse;
    private void OpenCreate() { if (CanCreate && CheckSession()) { editingCourse = null; showCreate = true; } }
    private void OpenEdit(CourseSearchResponse course) { if (CanCreate && !busy && !loadingCatalogs && CheckSession()) { editingCourse = course; showCreate = true; } }
    private void CloseCreate() { showCreate = false; editingCourse = null; }
    private async Task CourseSavedAsync(CourseCreateResponse response)
    {
        CloseCreate();
        await LoadAsync(page);
        if (!disposed && error is null && courses.Count == 0 && page > 1) await LoadAsync(page - 1);
        if (!disposed) status = error is null
            ? $"Curso actualizado. {response.Students.Count} estudiantes en la lista final · {response.RemovedRegistrationIds.Count} inscripciones desactivadas."
            : "El curso se actualizó. No se pudo refrescar el listado; pulsa Reintentar para consultarlo.";
    }
    private async Task CourseCreatedAsync(CourseCreateResponse response)
    {
        showCreate = false;
        await LoadAsync(1);
        if (!disposed) status = error is null ? "Curso e inscripciones creados correctamente." : "El curso y sus inscripciones se crearon. No se pudo actualizar el listado; pulsa Reintentar para consultarlo.";
    }
    private CourseSearchFilter DefaultFilters() => new() { TeacherId = IsTeacher ? Session.Current?.User?.Id.ToString("D") : null };
    private string ResultRange => courses.Count == 0 ? "0 resultados" : $"Mostrando {(page - 1) * loadedPageSize + 1}–{(page - 1) * loadedPageSize + courses.Count}";

    protected override async Task OnInitializedAsync()
    {
        if (!CheckSession()) return;
        filters = DefaultFilters();
        await Task.WhenAll(LoadAsync(1), LoadCatalogsAsync());
    }

    private bool CheckSession()
    {
        if (Session.Current is null)
        {
            if (!redirected) { redirected = true; Navigation.NavigateTo("/auth/login", replace: true); }
            return false;
        }
        if (!Session.IsInRole("Coordinator") && !IsTeacher)
        {
            error = "El listado de cursos está disponible para coordinadores y profesores.";
            return false;
        }
        return true;
    }

    private async Task SearchAsync()
    {
        appliedFilter = filters.Normalize();
        validationErrors.Clear();
        await LoadAsync(1);
    }

    private void ShowValidationErrors(EditContext context) => validationErrors = context.GetValidationMessages().Distinct().ToList();

    private async Task ClearFiltersAsync()
    {
        filters = DefaultFilters();
        appliedFilter = null;
        validationErrors.Clear();
        await LoadAsync(1);
    }

    private Task ChangePageSizeAsync() => LoadAsync(1);
    private Task PreviousPageAsync() => busy || page <= 1 ? Task.CompletedTask : LoadAsync(page - 1);
    private Task NextPageAsync() => busy || !hasNext ? Task.CompletedTask : LoadAsync(page + 1);
    private Task RetryAsync() => LoadAsync(page);

    private async Task LoadAsync(int requestedPage)
    {
        if (disposed || !CheckSession()) return;
        searchCancellation?.Cancel();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        searchCancellation = cancellation;
        var version = ++requestVersion;
        var requestedSize = pageSize;
        busy = true;
        error = status = null;
        try
        {
            // Reaplicar en cada consulta: buscar, limpiar, paginar y reintentar no pueden quitar el alcance del profesor.
            var requestFilter = appliedFilter;
            if (IsTeacher)
            {
                var teacherId = Session.Current?.User?.Id;
                if (teacherId is null || teacherId == Guid.Empty) throw new InvalidOperationException("No se pudo identificar al profesor de la sesión.");
                requestFilter = (requestFilter ?? new CourseSearchFilter()) with { TeacherId = teacherId.Value.ToString("D") };
            }
            var response = await CourseService.SearchAsync(new CourseSearchRequest { PageNumber = requestedPage, PageSize = requestedSize, Filter = requestFilter }, cancellation.Token);
            if (disposed || version != requestVersion) return;
            if (response?.Data is null || response.Error is { ErrorCode: not 0 })
                throw new InvalidOperationException("No se pudo obtener el listado de cursos. Inténtalo de nuevo.");
            // ClassController devuelve data.Count: filas de esta página, no el total.
            if (requestedPage > page && response.Data.Count == 0)
            {
                hasNext = false;
                status = "Has llegado al final del listado.";
                return;
            }
            courses = response.Data;
            page = requestedPage;
            loadedPageSize = requestedSize;
            hasNext = courses.Count == requestedSize;
        }
        catch (OperationCanceledException)
        {
            if (!disposed && version == requestVersion && !cancellation.IsCancellationRequested) error = "La consulta tardó demasiado. Inténtalo de nuevo.";
        }
        catch (HttpRequestException ex)
        {
            if (!disposed && version == requestVersion) error = HttpError(ex);
        }
        catch (JsonException)
        {
            if (!disposed && version == requestVersion) error = "No se pudo interpretar el listado recibido.";
        }
        catch (InvalidOperationException ex)
        {
            if (!disposed && version == requestVersion) error = ex.Message;
        }
        finally
        {
            if (version == requestVersion)
            {
                busy = false;
                if (error is not null)
                {
                    courses = [];
                    hasNext = false;
                    page = requestedPage;
                }
                searchCancellation = null;
            }
            cancellation.Dispose();
        }
    }

    private async Task LoadCatalogsAsync()
    {
        if (disposed || loadingCatalogs || !CheckSession()) return;
        loadingCatalogs = true;
        catalogError = null;
        try
        {
            var messages = await Task.WhenAll(
                LoadCatalogAsync("módulos", async () => modules = await ReadCatalogAsync(UserService.GetModulesAsync, item => item.Id, item => item.Name)),
                IsTeacher ? Task.FromResult<string?>(null) : LoadCatalogAsync("profesores", async () => teachers = await ReadCatalogAsync<UserSearchResponse>((number, token) => UserService.SearchAsync(new UserSearchRequest
                {
                    PageNumber = number, PageSize = 100, Filter = new UserSearchFilter { RoleId = TeacherRoleId }
                }, token), item => item.Id, TeacherName)),
                LoadCatalogAsync("aulas", async () => classrooms = await ReadCatalogAsync(CourseService.GetClassroomsAsync, item => item.Id, item => item.Name)));
            if (!disposed)
            {
                var failures = messages.Where(message => message is not null).ToArray();
                catalogError = failures.Length == 0 ? null : string.Join(" ", failures);
            }
        }
        finally { loadingCatalogs = false; }
    }

    private async Task<string?> LoadCatalogAsync(string name, Func<Task> load)
    {
        try { await load(); return null; }
        catch (HttpRequestException ex) { return disposed ? null : $"{name}: {HttpError(ex)}"; }
        catch (OperationCanceledException) { return disposed ? null : $"La carga de {name} tardó demasiado."; }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return disposed ? null : $"No se pudo cargar el catálogo de {name}.";
        }
    }
    private async Task<List<T>> ReadCatalogAsync<T>(Func<int, CancellationToken, Task<ApiResponse<List<T>>?>> fetch, Func<T, Guid> id, Func<T, string> name)
    {
        var cancellationToken = lifetime.Token;
        var result = new Dictionary<Guid, T>();
        for (var catalogPage = 1; ; catalogPage++)
        {
            var response = await fetch(catalogPage, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException("Catálogo no disponible.");
            var added = 0;
            foreach (var item in response.Data.Where(item => id(item) != Guid.Empty))
                if (result.TryAdd(id(item), item)) added++;
            if (response.Data.Count < 100) break;
            if (added == 0) throw new InvalidOperationException("El catálogo no avanzó a la siguiente página.");
        }
        return result.Values.OrderBy(name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private string HttpError(HttpRequestException exception)
    {
        if (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            Session.Clear();
            CheckSession();
            return "Tu sesión venció. Vuelve a iniciar sesión.";
        }
        return exception.StatusCode switch
        {
            HttpStatusCode.Forbidden => "No tienes permiso para consultar estos datos.",
            HttpStatusCode.BadRequest => "Revisa los filtros e inténtalo de nuevo.",
            HttpStatusCode.TooManyRequests => "Se realizaron demasiadas consultas. Espera un momento e inténtalo de nuevo.",
            _ => "No se pudo conectar con el servicio de cursos. Inténtalo de nuevo."
        };
    }

    private static string TeacherName(UserSearchResponse teacher) => $"{teacher.FirstName} {teacher.LastName}".Trim();
    private long? PageStudents => courses.Count == 0 || courses.Any(course => course.StudentCount is null) ? null : courses.Sum(course => course.StudentCount);
    private decimal? PageAverage
    {
        get
        {
            var evaluated = courses.Where(course => course.Exam1Average.HasValue && course.Exam2Average.HasValue && course.StudentCount > 0).ToArray();
            var students = evaluated.Sum(course => course.StudentCount);
            return students is null or 0 ? null : evaluated.Sum(course => (course.Exam1Average!.Value + course.Exam2Average!.Value) * course.StudentCount!.Value) / (students.Value * 2m);
        }
    }
    private IGrouping<Guid?, CourseSearchResponse>? TopTeacher => courses.Where(course => course.TeacherId.HasValue).GroupBy(course => course.TeacherId).OrderByDescending(group => group.Count()).FirstOrDefault();
    private static string Display(string? value, string fallback) => CourseDisplay.Text(value, fallback);
    private static string Hour(string? value) => CourseDisplay.Hour(value);
    private static string Bimester(int value) => value switch { 1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", 6 => "VI", _ => value.ToString() };

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        requestVersion++;
        searchCancellation?.Cancel();
        lifetime.Cancel();
        lifetime.Dispose();
    }
}

