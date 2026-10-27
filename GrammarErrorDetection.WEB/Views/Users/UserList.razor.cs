using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Views.Users;

public partial class UserList : IDisposable
{
    [Parameter] public CourseSearchResponse? Course { get; set; }
    private bool IsCourseList => Course is not null;
    private bool CanManageUsers => !IsCourseList && Session.IsInRole("Coordinator");
    private int ColumnCount => IsCourseList ? 7 : 6;
    private bool CanOpenGrades => CanRegisterGrades;
    private bool CanRegisterGrades => IsCourseList && Session.Current?.User is { } user
        && (Session.IsInRole("Coordinator") || (Session.IsInRole("Teacher") && Course!.TeacherId == user.Id));
    private bool CanAssignPoints => CanManageUsers || CanRegisterGrades;
    private UserSearchResponse? pointsUser;
    [Inject] private GrammarErrorDetection.Services.Services.Points.PointRegistrationResolver PointRegistrations { get; set; } = default!;
    private Guid pointsRegistrationId;
    private string pointsResolvedCourseName = "";
    private bool resolvingPoints;
    private string PointsCourseName => pointsResolvedCourseName;
    private async Task OpenPoints(UserSearchResponse user)
    {
        if (!CanAssignPoints || busy || resolvingPoints || !CheckSession()) return;
        var version = requestVersion;
        resolvingPoints = true; status = null;
        try
        {
            var classId = IsCourseList ? Course!.Id : user.CurrentCourse?.ClassId;
            var target = await PointRegistrations.ResolveAsync(user.Id, classId, lifetime.Token);
            if (disposed || version != requestVersion || !CheckSession()) return;
            pointsRegistrationId = target.RegistrationId;
            pointsResolvedCourseName = IsCourseList ? $"{Course!.ModuleName} · Paralelo {Course.Parallel}"
                : user.CurrentCourse is not null ? CurrentCourse(user) : target.CourseName;
            pointsUser = user;
        }
        catch (HttpRequestException ex) { if (!disposed && version == requestVersion) status = ex.StatusCode == HttpStatusCode.Forbidden ? "La API no permitió consultar las inscripciones de este usuario o curso." : HttpError(ex); }
        catch (OperationCanceledException) { if (!disposed && version == requestVersion) status = "La consulta de la inscripción tardó demasiado. Inténtalo de nuevo."; }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException) { if (!disposed && version == requestVersion) status = ex is JsonException ? "No se pudo interpretar la inscripción." : ex.Message; }
        finally { resolvingPoints = false; }
    }
    private void ClosePoints() { pointsUser = null; pointsRegistrationId = Guid.Empty; pointsResolvedCourseName = ""; }
    private void PointsSaved(GrammarErrorDetection.Models.Points.PointTransactionResponse transaction)
    {
        status = $"Movimiento de {transaction.Amount} puntos registrado para {FullName(pointsUser!)}.";
        ClosePoints();
    }
    private UserSearchResponse? gradingUser;
    private readonly Dictionary<Guid, StudentGradeDraft> gradeDrafts = [];
    private readonly Dictionary<Guid, Guid> gradeRegistrations = [];
    private bool loadingGrades;
    private string? gradesError;
    [Inject] private ICourseService GradeCourseService { get; set; } = default!;
    [Inject] private IEvaluationService EvaluationService { get; set; } = default!;
    private StudentGradeDraft GradeFor(UserSearchResponse user)
    {
        if (!gradeDrafts.TryGetValue(user.Id, out var draft)) gradeDrafts[user.Id] = draft = new();
        return draft;
    }
    private async Task OpenGrades(UserSearchResponse user)
    {
        if (!CanOpenGrades || busy || loadingGrades || !CheckSession()) return;
        if (!CanRegisterGrades || busy || loadingGrades || gradesError is not null || !gradeRegistrations.TryGetValue(user.Id, out var registrationId) || !CheckSession()) return;
        var version = requestVersion;
        loadingGrades = true;
        try
        {
            var evaluations = await EvaluationService.GetAsync(registrationId, lifetime.Token);
            if (disposed || version != requestVersion) return;
            var draft = new StudentGradeDraft();
            foreach (var group in evaluations.Where(item => item.Period is 1 or 2).GroupBy(item => item.Period))
            {
                if (group.Count() != 1) throw new InvalidOperationException("Hay evaluaciones duplicadas para el mismo examen.");
                draft.Set(group.Single());
            }
            gradeDrafts[user.Id] = draft;
            gradingUser = user;
        }
        catch (HttpRequestException ex) { if (!disposed && version == requestVersion) gradesError = HttpError(ex); }
        catch (OperationCanceledException) { if (!disposed && version == requestVersion) gradesError = "La consulta de notas se canceló. Inténtalo de nuevo."; }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException)
        { if (!disposed && version == requestVersion) gradesError = ex is InvalidOperationException ? ex.Message : "No se pudieron interpretar las notas."; }
        finally { if (version == requestVersion) loadingGrades = false; }
    }
    private void CloseGrades() => gradingUser = null;
    private void SaveGrades(StudentGradeDraft draft)
    {
        if (!CanRegisterGrades || gradingUser is null) return;
        gradeDrafts[gradingUser.Id] = draft;
        status = $"Notas de {FullName(gradingUser)} guardadas.";
    }
    private string ListTitle => IsCourseList ? "Estudiantes del curso" : "Listado de usuarios";
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private UserSearchFilter filters = new();
    private UserSearchFilter? appliedFilter;
    private List<UserSearchResponse> users = [];
    private List<UserLookupOption> roles = [], modules = [];
    private UserSearchResponse? editingUser, statusUser;
    private bool updatingStatus;
    private string? statusError;
    private int page = 1, pageSize = 20, loadedPageSize = 20, requestVersion;
    private bool busy, loadingCatalogs, hasNext, disposed, redirected;
    private string? error, catalogError, status;
    private List<string> validationErrors = [];
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchCancellation;
    private string ResultRange => users.Count == 0 ? "0 resultados" : $"Mostrando {(page - 1) * loadedPageSize + 1}–{(page - 1) * loadedPageSize + users.Count}";

    protected override async Task OnInitializedAsync()
    {
        if (!CheckSession()) return;
        if (IsCourseList) await LoadAsync(1);
        else await Task.WhenAll(LoadAsync(1), LoadCatalogsAsync());
    }

    private bool CheckSession()
    {
        if (Session.Current is null)
        {
            if (!redirected) { redirected = true; Navigation.NavigateTo("/auth/login", replace: true); }
            return false;
        }
        var isTeacher = Session.IsInRole("Teacher");
        var courseAllowed = Course is { Id: var id } && id != Guid.Empty
            && ((Session.IsInRole("Coordinator") && !isTeacher) || (isTeacher && Course.TeacherId == Session.Current?.User?.Id));
        if ((IsCourseList && !courseAllowed) || (!IsCourseList && !Session.IsInRole("Coordinator")))
        {
            error = IsCourseList ? "No tienes acceso a los estudiantes de este curso." : "El listado de usuarios está disponible para coordinadores.";
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
        filters = new();
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
            // El contexto del curso se impone en cada petición, incluso al limpiar o cambiar de página.
            var requestFilter = IsCourseList ? (appliedFilter ?? new UserSearchFilter()) with { ClassId = Course!.Id, RoleId = null, CurrentModuleNameId = null } : appliedFilter;
            var response = await UserService.SearchAsync(new UserSearchRequest { PageNumber = requestedPage, PageSize = requestedSize, Filter = requestFilter }, cancellation.Token);
            if (disposed || version != requestVersion) return;
            if (response?.Data is null || response.Error is { ErrorCode: not 0 })
                throw new InvalidOperationException("No se pudo obtener el listado de usuarios. Inténtalo de nuevo.");
            // Count representa las filas de esta página en el backend actual, no el total.
            if (requestedPage > page && response.Data.Count == 0)
            {
                hasNext = false;
                status = "Has llegado al final del listado.";
                return;
            }
            users = response.Data;
            page = requestedPage;
            loadedPageSize = requestedSize;
            hasNext = users.Count == requestedSize;
            if (CanRegisterGrades) await LoadGradesAsync(cancellation.Token, version);
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
                    users = [];
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
        if (disposed || loadingCatalogs || IsCourseList || !CheckSession()) return;
        loadingCatalogs = true;
        catalogError = null;
        try
        {
            var lists = await Task.WhenAll(ReadCatalogAsync(UserService.GetRolesAsync), ReadCatalogAsync(UserService.GetModulesAsync));
            if (disposed) return;
            roles = lists[0];
            modules = lists[1];
        }
        catch (HttpRequestException ex) { if (!disposed) catalogError = HttpError(ex); }
        catch (OperationCanceledException) { if (!disposed) catalogError = "Los filtros de rol y módulo tardaron demasiado en cargar."; }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            if (!disposed) catalogError = "No se pudieron cargar los filtros de rol y módulo. Puedes usar los demás filtros.";
        }
        finally { loadingCatalogs = false; }
    }

    private async Task<List<UserLookupOption>> ReadCatalogAsync(Func<int, CancellationToken, Task<ApiResponse<List<UserLookupOption>>?>> fetch)
    {
        var cancellationToken = lifetime.Token;
        var result = new Dictionary<Guid, UserLookupOption>();
        for (var catalogPage = 1; ; catalogPage++)
        {
            var response = await fetch(catalogPage, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException("Catálogo no disponible.");
            var added = 0;
            foreach (var item in response.Data.Where(item => item.Id != Guid.Empty))
                if (result.TryAdd(item.Id, item)) added++;
            if (response.Data.Count < 100) break;
            if (added == 0) throw new InvalidOperationException("El catálogo no avanzó a la siguiente página.");
        }
        return result.Values.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
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
            _ => "No se pudo conectar con el servicio de usuarios. Inténtalo de nuevo."
        };
    }

    private static string FullName(UserSearchResponse user) => $"{user.FirstName} {user.LastName}".Trim();
    private static string Initials(UserSearchResponse user) => $"{user.FirstName?.FirstOrDefault()}{user.LastName?.FirstOrDefault()}".Replace("\0", "");
    private static string PreviousCourse(UserSearchResponse user) => user.LastCourse?.ModuleName ?? "Sin curso anterior";
    private static string CurrentCourse(UserSearchResponse user) => user.CurrentCourse is { } course ? $"{course.ModuleName} · Paralelo {course.Parallel ?? "—"}" : "Sin curso actual";
    private static string Schedule(UserSearchResponse user) => user.CurrentCourse is { StartTime: { } start, EndTime: { } end } ? $"{start:HH:mm} – {end:HH:mm}" : "Sin horario asignado";
    private decimal? CourseGrade(UserSearchResponse user) => user.CurrentCourse?.ClassId == Course?.Id ? user.CurrentCourseFinalScore : null;
    private string RegistrationStatus(UserSearchResponse user) => user.CurrentCourse?.ClassId == Course?.Id ? user.CurrentCourse?.RegistrationStatus ?? "No informado" : "No informado";
    private void ViewProfile(UserSearchResponse user)
    {
        if (CanManageUsers && CheckSession()) Navigation.NavigateTo($"/users/{user.Id:D}");
    }

    private void OpenEdit(UserSearchResponse user)
    {
        if (CanManageUsers && !busy && !updatingStatus && CheckSession()) editingUser = user;
    }

    private void CloseEdit() => editingUser = null;

    private void OpenStatus(UserSearchResponse user)
    {
        if (!CanManageUsers || busy || updatingStatus || !CheckSession()) return;
        statusUser = user;
        statusError = null;
    }

    private void CloseStatus() { if (!updatingStatus) statusUser = null; }

    private async Task UserSavedAsync(UserUpdateResponse response)
    {
        editingUser = null;
        await RefreshAfterUpdateAsync("Información del usuario actualizada.");
    }

    private async Task RefreshAfterUpdateAsync(string message)
    {
        await LoadAsync(page);
        if (!disposed && error is null && users.Count == 0 && page > 1) await LoadAsync(page - 1);
        if (!disposed) status = error is null ? message : $"{message} No se pudo refrescar el listado; pulsa Reintentar para consultarlo de nuevo.";
    }

    private async Task ConfirmStatusAsync()
    {
        if (!CanManageUsers || statusUser is null || updatingStatus || disposed || !CheckSession()) return;
        var user = statusUser;
        var active = !user.Active;
        updatingStatus = true;
        statusError = null;
        try
        {
            var response = await UserService.UpdateAsync(new UserUpdateRequest { Id = user.Id, Active = active }, lifetime.Token);
            if (disposed) return;
            if (response?.Data is null || response.Data.Id != user.Id || response.Data.Active != active || response.Error is { ErrorCode: not 0 })
            {
                statusError = "No se pudo confirmar el cambio de estado. Inténtalo de nuevo.";
                return;
            }
            statusUser = null;
            await RefreshAfterUpdateAsync(active ? "Usuario reactivado." : "Usuario desactivado.");
        }
        catch (HttpRequestException ex) { if (!disposed) statusError = HttpError(ex); }
        catch (OperationCanceledException) { if (!disposed) statusError = "El cambio de estado tardó demasiado. Inténtalo de nuevo."; }
        catch (JsonException) { if (!disposed) statusError = "No se pudo interpretar la respuesta del cambio de estado."; }
        finally { updatingStatus = false; }
    }

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
