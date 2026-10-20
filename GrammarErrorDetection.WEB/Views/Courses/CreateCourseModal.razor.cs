using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Views.Courses;

public partial class CreateCourseModal : IDisposable
{
    [Parameter] public IReadOnlyList<UserLookupOption> Modules { get; set; } = [];
    [Parameter] public IReadOnlyList<UserSearchResponse> Teachers { get; set; } = [];
    [Parameter] public IReadOnlyList<ClassroomLookupOption> Classrooms { get; set; } = [];
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<CourseCreateResponse> OnCreated { get; set; }
    [Parameter] public CourseSearchResponse? Course { get; set; }
    [Parameter] public EventCallback<CourseCreateResponse> OnSaved { get; set; }
    [Inject] private ICourseService CourseService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private CourseWriteRequest model = new CourseCreateRequest();
    private EditContext editContext = default!;
    private readonly string formId = $"create-course-{Guid.NewGuid():N}";
    private readonly Dictionary<Guid, EligibleStudent> selected = [];
    private readonly Dictionary<Guid, EligibleStudent> originalStudents = [];
    private bool IsEdit => Course is not null;
    private bool initializing, membersLoaded, removalConfirmed;
    private string? initializationError;
    private int RemovedCount => originalStudents.Keys.Count(id => !selected.ContainsKey(id));
    private int RetainedCount => originalStudents.Keys.Count(selected.ContainsKey);
    private bool NeedsTransfer(EligibleStudent student) => student.RequiresTransfer && !(IsEdit && originalStudents.ContainsKey(student.UserId));
    private List<EligibleStudent> eligible = [];
    private List<string> validationErrors = [];
    private string? error, eligibilityError, eligibilityStatus;
    private string firstName = "", lastName = "", ci = "";
    private string appliedFirstName = "", appliedLastName = "", appliedCi = "";
    private bool saving, loading, hasNext, disposed, transferConfirmed;
    private int page = 1, requestVersion;
    private const int PageSize = 20;
    private (Guid, int, int) Scope => (model.ModuleNameId, model.Year, model.Bimester);
    private (Guid, int, int)? loadedScope;
    private bool ValidScope => model.ModuleNameId != Guid.Empty && model.Year is >= 1 and <= 9999 && model.Bimester is >= 1 and <= 6;
    private int TransferCount => selected.Values.Count(NeedsTransfer);
    private string SelectionSummary => $"{selected.Count} {(selected.Count == 1 ? "estudiante" : "estudiantes")} · {TransferCount} {(TransferCount == 1 ? "traslado" : "traslados")}" + (IsEdit ? $" · {RemovedCount} {(RemovedCount == 1 ? "baja" : "bajas")}" : "");
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? eligibilityCancellation;

    protected override async Task OnInitializedAsync()
    {
        if (Course is { } course)
        {
            model = new CourseUpdateRequest
            {
                Id = course.Id, TeacherId = course.TeacherId ?? Guid.Empty, ModuleNameId = course.ModuleNameId,
                ClassroomId = course.ClassroomId ?? Guid.Empty, Year = course.Year, Bimester = course.Bimester,
                StartDate = course.StartDate?.DateTime, EndDate = course.EndDate?.DateTime,
                Parallel = course.Parallel ?? "", Active = course.Active,
                StartTime = ParseTime(course.StartTime), EndTime = ParseTime(course.EndTime)
            };
        }
        editContext = new EditContext(model);
        if (IsEdit) await LoadCurrentStudentsAsync();
    }

    private async Task LoadCurrentStudentsAsync()
    {
        if (initializing || saving || disposed || !IsEdit || !CheckPermission()) return;
        initializing = true;
        membersLoaded = false;
        initializationError = null;
        selected.Clear(); originalStudents.Clear();
        try
        {
            var registrations = new HashSet<Guid>();
            var students = new Dictionary<Guid, EligibleStudent>();
            for (var number = 1; ; number++)
            {
                var response = await CourseService.GetRegistrationsAsync(Course!.Id, number, lifetime.Token);
                if (disposed) return;
                if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException("No se pudieron cargar las inscripciones actuales.");
                var added = 0;
                foreach (var registration in response.Data)
                {
                    if (registration.Id == Guid.Empty || registration.UserId == Guid.Empty || registration.ClassId != Course.Id || !registration.Active)
                        throw new InvalidOperationException("Las inscripciones recibidas no corresponden al curso. Reintenta la consulta.");
                    if (registrations.Add(registration.Id)) added++;
                    students.TryAdd(registration.UserId, new EligibleStudent { UserId = registration.UserId, IsAssigned = true,
                        Assignments = [new() { ClassId = Course.Id, RegistrationId = registration.Id, ModuleNameId = Course.ModuleNameId, ModuleName = Course.ModuleName, Parallel = Course.Parallel, StartTime = Course.StartTime, EndTime = Course.EndTime }] });
                }
                if (response.Data.Count < 100) break;
                if (added == 0) throw new InvalidOperationException("La consulta de inscripciones no avanzó de página.");
            }
            // Completar nombres sin depender de la primera página de elegibles.
            if (students.Count > 0)
            {
                var seen = new HashSet<Guid>();
                for (var number = 1; ; number++)
                {
                    var response = await CourseService.GetEligibleStudentsAsync(new() { ModuleNameId = Course!.ModuleNameId, Year = Course.Year, Bimester = Course.Bimester, IsAssigned = true, PageNumber = number, PageSize = 100 }, lifetime.Token);
                    if (disposed) return;
                    if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException("No se pudieron cargar los estudiantes actuales.");
                    var added = 0;
                    foreach (var student in response.Data)
                    {
                        if (seen.Add(student.UserId)) added++;
                        if (students.ContainsKey(student.UserId)) students[student.UserId] = student;
                    }
                    if (response.Data.Count < 100 || students.Keys.All(seen.Contains)) break;
                    if (added == 0) throw new InvalidOperationException("La consulta de estudiantes no avanzó de página.");
                }
            }
            foreach (var student in students.Values) { originalStudents.Add(student.UserId, student); selected.Add(student.UserId, student); }
            membersLoaded = true;
        }
        catch (HttpRequestException ex) { if (!disposed) initializationError = HttpError(ex, false); }
        catch (OperationCanceledException) { if (!disposed) initializationError = "La carga de inscripciones tardó demasiado. Reintenta para editar con la lista completa."; }
        catch (JsonException) { if (!disposed) initializationError = "No se pudieron interpretar las inscripciones actuales."; }
        catch (InvalidOperationException ex) { if (!disposed) initializationError = ex.Message; }
        finally { initializing = false; }
        if (membersLoaded && !disposed) await LoadEligibleAsync(1);
    }

    private bool CheckPermission()
    {
        if (Session.Current is null) { Navigation.NavigateTo("/auth/login", replace: true); return false; }
        if (!Session.IsInRole("Coordinator") || Session.IsInRole("Teacher")) { error = "No tienes permiso para gestionar cursos."; return false; }
        return true;
    }

    private async Task ScopeChangedAsync()
    {
        selected.Clear();
        transferConfirmed = false;
        removalConfirmed = false;
        eligible = [];
        loadedScope = null;
        page = 1;
        hasNext = false;
        error = eligibilityError = eligibilityStatus = null;
        eligibilityCancellation?.Cancel();
        requestVersion++;
        loading = false;
        if (ValidScope) await LoadEligibleAsync(1);
    }

    private async Task SearchEligibleAsync()
    {
        if (saving || loading) return;
        appliedFirstName = firstName.Trim(); appliedLastName = lastName.Trim(); appliedCi = ci.Trim();
        await LoadEligibleAsync(1);
    }

    private async Task LoadEligibleAsync(int requestedPage)
    {
        if (saving || initializing || (IsEdit && !membersLoaded) || disposed || !ValidScope || !CheckPermission()) return;
        eligibilityCancellation?.Cancel();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        eligibilityCancellation = cancellation;
        var version = ++requestVersion;
        var scope = Scope;
        loading = true;
        eligibilityError = eligibilityStatus = null;
        try
        {
            var response = await CourseService.GetEligibleStudentsAsync(new()
            {
                ModuleNameId = scope.Item1, Year = scope.Item2, Bimester = scope.Item3,
                PageNumber = requestedPage, PageSize = PageSize,
                FirstName = appliedFirstName, LastName = appliedLastName, CI = appliedCi
            }, cancellation.Token);
            if (disposed || version != requestVersion || scope != Scope) return;
            if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException("No se pudo consultar a los estudiantes elegibles.");
            if (requestedPage > page && response.Data.Count == 0)
            {
                hasNext = false;
                eligibilityStatus = "Has llegado al final del listado.";
                return;
            }
            eligible = response.Data.Where(student => student.UserId != Guid.Empty).DistinctBy(student => student.UserId).ToList();
            foreach (var student in eligible.Where(student => selected.ContainsKey(student.UserId)))
            {
                if (NeedsTransfer(student) != NeedsTransfer(selected[student.UserId])) transferConfirmed = false;
                selected[student.UserId] = student;
            }
            loadedScope = scope;
            page = requestedPage;
            hasNext = response.Data.Count == PageSize;
        }
        catch (OperationCanceledException) { if (!disposed && version == requestVersion && !cancellation.IsCancellationRequested) eligibilityError = "La consulta tardó demasiado. Inténtalo de nuevo."; }
        catch (HttpRequestException ex) { if (!disposed && version == requestVersion) eligibilityError = HttpError(ex, false); }
        catch (JsonException) { if (!disposed && version == requestVersion) eligibilityError = "No se pudo interpretar la respuesta de estudiantes elegibles."; }
        catch (InvalidOperationException ex) { if (!disposed && version == requestVersion) eligibilityError = ex.Message; }
        finally
        {
            if (version == requestVersion)
            {
                loading = false;
                eligibilityCancellation = null;
                if (eligibilityError is not null) { eligible = []; hasNext = false; }
            }
            cancellation.Dispose();
        }
    }

    private void Toggle(EligibleStudent student)
    {
        if (saving || loading || initializing || (IsEdit && !membersLoaded)) return;
        if (!selected.Remove(student.UserId)) selected.Add(student.UserId, student);
        transferConfirmed = false;
        removalConfirmed = false;
    }

    private void ClearSelection() { if (!saving && !initializing) { selected.Clear(); transferConfirmed = removalConfirmed = false; } }

    private async Task SaveAsync()
    {
        if (saving || loading || initializing || (IsEdit && !membersLoaded) || disposed || !CheckPermission()) return;
        error = null;
        model.StudentIds = selected.Keys.ToList();
        model.Parallel = (model.Parallel ?? "").Trim();
        validationErrors.Clear();
        if (!editContext.Validate()) { validationErrors = editContext.GetValidationMessages().Distinct().ToList(); return; }
        if (selected.Count > 0 && (loadedScope != Scope || eligibilityError is not null)) { error = "Consulta los estudiantes elegibles para el módulo y periodo seleccionados."; return; }
        if (TransferCount > 0 && !transferConfirmed) { error = "Confirma el traslado de los estudiantes que ya tienen un curso asignado."; return; }
        if (RemovedCount > 0 && !removalConfirmed) { error = "Confirma que quieres quitar a los estudiantes omitidos de la selección final."; return; }
        saving = true;
        try
        {
            var response = model is CourseUpdateRequest update
                ? await CourseService.UpdateWithStudentsAsync(update, lifetime.Token)
                : await CourseService.CreateWithStudentsAsync((CourseCreateRequest)model, lifetime.Token);
            if (disposed) return;
            if (response?.Data?.Class is not { Id: var id } || id == Guid.Empty || response.Error is { ErrorCode: not 0 }
                || (IsEdit && (id != Course!.Id || response.Data.Students is null || !model.StudentIds.ToHashSet().SetEquals(response.Data.Students.Select(student => student.UserId)))))
            {
                error = "No se pudo confirmar el guardado. Revisa el listado antes de volver a enviar el formulario.";
                return;
            }
            if (IsEdit) await OnSaved.InvokeAsync(response.Data); else await OnCreated.InvokeAsync(response.Data);
        }
        catch (HttpRequestException ex) { if (!disposed) error = HttpError(ex, true); }
        catch (OperationCanceledException) { if (!disposed) error = "No se pudo confirmar el guardado a tiempo. Revisa el listado antes de volver a enviar."; }
        catch (JsonException) { if (!disposed) error = "No se pudo interpretar la respuesta. Revisa el listado antes de volver a enviar."; }
        finally { saving = false; }
    }

    private string HttpError(HttpRequestException exception, bool creating)
    {
        if (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true);
            return "Tu sesión venció. Vuelve a iniciar sesión.";
        }
        return exception.StatusCode switch
        {
            HttpStatusCode.Forbidden => "No tienes permiso para realizar esta operación.",
            HttpStatusCode.BadRequest => "Revisa los datos del curso y la selección de estudiantes.",
            HttpStatusCode.Conflict => "La asignación o disponibilidad cambió. Revisa los estudiantes y el horario antes de volver a enviar.",
            _ => creating ? "No se pudo confirmar el guardado. Revisa el listado antes de volver a enviar." : "No se pudo cargar el listado. Inténtalo de nuevo."
        };
    }

    private static string FullName(EligibleStudent student) => string.IsNullOrWhiteSpace($"{student.FirstName} {student.LastName}".Trim()) ? $"Estudiante {student.UserId:D}" : $"{student.FirstName} {student.LastName}".Trim();
    private static TimeOnly ParseTime(string? value) => TimeOnly.TryParse(value?.TrimEnd('Z', 'z'), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var time) ? time : default;
    private static string Initials(EligibleStudent student) => $"{student.FirstName.FirstOrDefault()}{student.LastName.FirstOrDefault()}".Replace("\0", "");
    private static string AssignmentHour(string? value) => TimeOnly.TryParse(value?.TrimEnd('Z', 'z'), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var time) ? time.ToString("HH:mm") : "—";
    public void Dispose()
    {
        disposed = true;
        requestVersion++;
        eligibilityCancellation?.Cancel();
        lifetime.Cancel(); lifetime.Dispose();
    }
}
