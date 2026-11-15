using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GrammarErrorDetection.Models.Registrations;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using static GrammarErrorDetection.WEB.Views.Registrations.RegistrationViewSupport;

namespace GrammarErrorDetection.WEB.Views.Registrations;

public partial class ApplyRegistrationModal : IDisposable
{
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<RegistrationResponse> OnSaved { get; set; }
    [Inject] private IRegistrationService Registrations { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private readonly AvailableRegistrationCoursesRequest period = new() { Year = DateTime.Today.Year, Bimester = (DateTime.Today.Month + 1) / 2 };
    private readonly RegistrationApplyRequest application = new();
    private List<AvailableRegistrationCourse> courses = [];
    private AvailableRegistrationCourse? selected;
    private string? courseError, saveError;
    private int page = 1, loadedYear, loadedBimester;
    private bool loading, saving, loaded, hasNext, disposed;
    private Guid owner;
    private bool Busy => loading || saving;
    private bool Allowed => Session.Current?.User?.Id == owner && Session.IsInRole("Student");

    protected override async Task OnInitializedAsync()
    {
        owner = Session.Current?.User?.Id ?? Guid.Empty;
        await LoadAsync(1);
    }
    private void InvalidateCourses()
    {
        courses = []; selected = null; loaded = false; hasNext = false;
        courseError = saveError = null; page = 1;
    }
    private Task SearchAsync() => LoadAsync(1);
    private Task RetryAsync() => LoadAsync(page);
    private Task PreviousAsync() => LoadAsync(Math.Max(1, page - 1));
    private Task NextAsync() => hasNext ? LoadAsync(page + 1) : Task.CompletedTask;
    private async Task LoadAsync(int requestedPage)
    {
        if (disposed || Busy || !Allowed) return;
        var validation = new List<ValidationResult>();
        if (!Validator.TryValidateObject(period, new(period), validation, true))
        {
            InvalidateCourses();
            courseError = string.Join(" ", validation.Select(item => item.ErrorMessage));
            return;
        }
        loading = true;
        courses = []; selected = null; loaded = false; hasNext = false; courseError = saveError = null;
        page = requestedPage;
        loadedYear = period.Year; loadedBimester = period.Bimester;
        try
        {
            var data = Data(await Registrations.GetAvailableCoursesAsync(new()
            {
                PageNumber = page, PageSize = period.PageSize, Year = loadedYear, Bimester = loadedBimester
            }, lifetime.Token));
            if (disposed || !Allowed) return;
            if (data.Any(item => item.ClassId == Guid.Empty || item.Year != loadedYear || item.Bimester != loadedBimester))
                throw new InvalidOperationException("El servicio devolvió cursos que no corresponden al periodo consultado.");
            courses = data;
            hasNext = data.Count == period.PageSize;
            loaded = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        {
            if (!disposed) { courseError = Error(ex); HandleUnauthorized(ex); }
        }
        finally { loading = false; }
    }
    private void Select(AvailableRegistrationCourse course)
    {
        if (Busy || course.AvailableSeats <= 0) return;
        selected = course;
        saveError = null;
    }
    private async Task SaveAsync()
    {
        if (disposed || Busy || !Allowed || selected is null) return;
        if (selected.Year != period.Year || selected.Bimester != period.Bimester || !courses.Contains(selected))
        { saveError = "Consulta nuevamente los cursos del periodo elegido."; return; }
        saving = true;
        saveError = null;
        try
        {
            var response = Data(await Registrations.ApplyAsync(new() { ClassId = selected.ClassId }, lifetime.Token));
            if (disposed || !Allowed) return;
            if (response.Id == Guid.Empty || response.UserId != owner || response.ClassId != selected.ClassId || !IsPending(response))
                throw new InvalidOperationException("No se pudo confirmar la solicitud. Consulta Mis inscripciones antes de volver a intentarlo.");
            await OnSaved.InvokeAsync(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        {
            if (!disposed) { saveError = Error(ex); HandleUnauthorized(ex); }
        }
        finally { saving = false; }
    }
    private void HandleUnauthorized(Exception ex)
    {
        if (ex is not HttpRequestException { StatusCode: System.Net.HttpStatusCode.Unauthorized }) return;
        Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true);
    }
    private Task CloseAsync() => Busy ? Task.CompletedTask : OnClose.InvokeAsync();
    public void Dispose() { disposed = true; lifetime.Cancel(); lifetime.Dispose(); }
}
