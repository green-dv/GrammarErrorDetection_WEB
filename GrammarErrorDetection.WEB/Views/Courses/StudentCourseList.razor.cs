using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using static GrammarErrorDetection.WEB.Views.Registrations.RegistrationViewSupport;

namespace GrammarErrorDetection.WEB.Views.Courses;

public partial class StudentCourseList : IDisposable
{
    [Inject] private IRegistrationService Registrations { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private List<StudentCourseResponse> courses = [];
    private Guid owner;
    private int page = 1, pageSize = 20;
    private bool loading, hasNext, disposed;
    private string? error;
    private bool Allowed => Session.Current?.User?.Id == owner && Session.IsInRole("Student");

    protected override async Task OnInitializedAsync()
    {
        owner = Session.Current?.User?.Id ?? Guid.Empty;
        Session.Changed += SessionChanged;
        await LoadAsync(1);
    }
    private void SessionChanged()
    {
        if (Session.Current?.User?.Id == owner) return;
        lifetime.Cancel(); courses = [];
        _ = InvokeAsync(StateHasChanged);
    }
    private Task RetryAsync() => LoadAsync(page);
    private Task ChangeSizeAsync() => LoadAsync(1);
    private Task PreviousAsync() => LoadAsync(Math.Max(1, page - 1));
    private Task NextAsync() => hasNext ? LoadAsync(page + 1) : Task.CompletedTask;
    private async Task LoadAsync(int requestedPage)
    {
        if (disposed || loading || !Allowed) return;
        loading = true; courses = []; error = null; hasNext = false; page = requestedPage;
        try
        {
            var data = Data(await Registrations.GetMyCoursesAsync(page, pageSize, lifetime.Token));
            if (disposed || !Allowed) return;
            hasNext = data.Count == pageSize;
            courses = data.Where(item => !string.Equals(item.Status, "Rejected", StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        {
            if (!disposed) { error = Error(ex); HandleUnauthorized(ex); }
        }
        finally { loading = false; }
    }
    private void HandleUnauthorized(Exception ex)
    {
        if (ex is not HttpRequestException { StatusCode: HttpStatusCode.Unauthorized }) return;
        Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true);
    }
    public void Dispose() { disposed = true; Session.Changed -= SessionChanged; lifetime.Cancel(); lifetime.Dispose(); }
}
