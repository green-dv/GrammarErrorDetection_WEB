using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;

namespace GrammarErrorDetection.WEB.Views.Courses;

public partial class TeacherCourseList : IDisposable
{
    [Inject] private ICourseService Courses { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private List<CourseSearchResponse> courses = [];
    private Guid owner;
    private int page = 1, pageSize = 20;
    private bool loading, hasNext, disposed;
    private string? error;
    private bool Allowed => owner != Guid.Empty && Session.Current?.User?.Id == owner && Session.IsInRole("Teacher");

    protected override async Task OnInitializedAsync()
    {
        owner = Session.Current?.User?.Id ?? Guid.Empty;
        Session.Changed += SessionChanged;
        await LoadAsync(1);
    }
    private void SessionChanged()
    {
        if (Allowed) return;
        lifetime.Cancel();
        courses = []; hasNext = false; error = null;
        _ = InvokeAsync(StateHasChanged);
    }
    private Task RetryAsync() => LoadAsync(page);
    private Task ChangeSizeAsync() => LoadAsync(1);
    private Task PreviousAsync() => LoadAsync(Math.Max(1, page - 1));
    private Task NextAsync() => hasNext ? LoadAsync(page + 1) : Task.CompletedTask;
    private async Task LoadAsync(int requestedPage)
    {
        if (disposed || loading || !Allowed || lifetime.IsCancellationRequested) return;
        loading = true; courses = []; error = null; hasNext = false; page = requestedPage;
        var token = lifetime.Token;
        try
        {
            // Keep the session teacher fixed on every page, size change and retry.
            var response = await Courses.SearchAsync(new()
            {
                PageNumber = requestedPage, PageSize = pageSize,
                Filter = new() { TeacherId = owner.ToString("D") }
            }, token);
            token.ThrowIfCancellationRequested();
            if (disposed || !Allowed) return;
            if (response?.Data is null || response.Error is { ErrorCode: not 0 })
                throw new InvalidOperationException(response?.Error?.ErrorMessage ?? "No se pudo consultar tus cursos.");
            if (response.Data.Any(course => course.TeacherId != owner))
                throw new InvalidOperationException("El servicio devolvió cursos que no corresponden al docente de la sesión.");
            courses = response.Data;
            hasNext = courses.Count == pageSize;
        }
        catch (HttpRequestException ex)
        {
            if (disposed || !Allowed) return;
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true);
            }
            else error = ex.StatusCode == HttpStatusCode.Forbidden ? "No tienes permiso para consultar estos cursos." : "No se pudo conectar con el servicio de cursos. Inténtalo de nuevo.";
        }
        catch (OperationCanceledException) { if (!disposed && Allowed && !token.IsCancellationRequested) error = "La consulta tardó demasiado. Inténtalo de nuevo."; }
        catch (JsonException) { if (!disposed && Allowed) error = "No se pudo interpretar el listado de cursos."; }
        catch (InvalidOperationException ex) { if (!disposed && Allowed) error = ex.Message; }
        finally { loading = false; }
    }
    public void Dispose() { disposed = true; Session.Changed -= SessionChanged; lifetime.Cancel(); lifetime.Dispose(); }
}
