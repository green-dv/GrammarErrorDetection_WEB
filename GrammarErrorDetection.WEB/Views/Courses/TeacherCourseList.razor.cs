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
    /// <summary>Docente cuyos cursos se muestran; sin valor, el de la sesión.</summary>
    [Parameter] public Guid? TeacherId { get; set; }
    private readonly CancellationTokenSource lifetime = new();
    private List<CourseSearchResponse> courses = [];
    private Guid owner;
    private int page = 1, pageSize = 20;
    private bool loading, hasNext, disposed;
    private long? total;
    private string PageOf => total is { } all ? I18n.T(" de {0}", Math.Max(1, (all + pageSize - 1) / pageSize)) : "";
    private string? error;
    private Guid Target => TeacherId is { } id && id != Guid.Empty ? id : owner;
    private bool IsOwn => Target == owner;
    private bool Allowed => owner != Guid.Empty && Session.Current?.User?.Id == owner
        && ((IsOwn && Session.IsInRole("Teacher")) || (!IsOwn && Session.IsInRole("Coordinator")));

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
        courses = []; total = null; hasNext = false; error = null;
        _ = InvokeAsync(StateHasChanged);
    }
    private Task RetryAsync() => LoadAsync(page);
    private Task ChangeSizeAsync() => LoadAsync(1);
    private Task PreviousAsync() => LoadAsync(Math.Max(1, page - 1));
    private Task NextAsync() => hasNext ? LoadAsync(page + 1) : Task.CompletedTask;
    private async Task LoadAsync(int requestedPage)
    {
        if (disposed || loading || !Allowed || lifetime.IsCancellationRequested) return;
        loading = true; courses = []; total = null; error = null; hasNext = false; page = requestedPage;
        var token = lifetime.Token;
        try
        {
            // Keep the target teacher fixed on every page, size change and retry.
            var response = await Courses.SearchAsync(new()
            {
                PageNumber = requestedPage, PageSize = pageSize,
                Filter = new() { TeacherId = Target.ToString("D") }
            }, token);
            token.ThrowIfCancellationRequested();
            if (disposed || !Allowed) return;
            if (response?.Data is null || response.Error is { ErrorCode: not 0 })
                throw new InvalidOperationException(response?.Error?.ErrorMessage ?? I18n.T("No se pudo consultar tus cursos."));
            if (response.Data.Any(course => course.TeacherId != Target))
                throw new InvalidOperationException(I18n.T("El servicio devolvió cursos que no corresponden al docente de la sesión."));
            courses = response.Data;
            total = response.TotalCount;
            hasNext = total is { } all ? (long)requestedPage * pageSize < all : courses.Count == pageSize;
        }
        catch (HttpRequestException ex)
        {
            if (disposed || !Allowed) return;
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true);
            }
            else error = ex.StatusCode == HttpStatusCode.Forbidden ? I18n.T("No tienes permiso para consultar estos cursos.") : I18n.T("No se pudo conectar con el servicio de cursos. Inténtalo de nuevo.");
        }
        catch (OperationCanceledException) { if (!disposed && Allowed && !token.IsCancellationRequested) error = I18n.T("La consulta tardó demasiado. Inténtalo de nuevo."); }
        catch (JsonException) { if (!disposed && Allowed) error = I18n.T("No se pudo interpretar el listado de cursos."); }
        catch (InvalidOperationException ex) { if (!disposed && Allowed) error = ex.Message; }
        finally { loading = false; }
    }
    public void Dispose() { disposed = true; Session.Changed -= SessionChanged; lifetime.Cancel(); lifetime.Dispose(); }
}
