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
    [Parameter] public bool Embedded { get; set; }
    [Parameter] public Guid? UserId { get; set; }
    [Inject] private IRegistrationService Registrations { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? requestCancellation;
    private List<StudentCourseResponse> courses = [];
    private Guid owner;
    private Guid? loadedUser;
    private int requestVersion;
    private int page = 1, pageSize = 20;
    private bool loading, hasNext, disposed;
    private string? error;
    private Guid TargetUser => UserId ?? owner;
    private bool IsOwn => TargetUser == owner;
    private string Title => IsOwn ? "Mis cursos" : "Cursos del estudiante";
    private bool Allowed => TargetUser != Guid.Empty && Session.Current?.User?.Id == owner &&
        ((IsOwn && Session.IsInRole("Student")) || Session.IsInRole("Coordinator"));

    protected override void OnInitialized()
    {
        owner = Session.Current?.User?.Id ?? Guid.Empty;
        Session.Changed += SessionChanged;
    }
    protected override async Task OnParametersSetAsync()
    {
        if (loadedUser == TargetUser) return;
        loadedUser = TargetUser;
        requestVersion++;
        requestCancellation?.Cancel();
        loading = false;
        courses = [];
        hasNext = false;
        error = null;
        await LoadAsync(1);
    }
    private void SessionChanged()
    {
        if (Allowed) return;
        requestVersion++;
        requestCancellation?.Cancel();
        courses = []; hasNext = false; loading = false; error = null;
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
        requestCancellation?.Dispose();
        requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        var token = requestCancellation.Token;
        var current = ++requestVersion;
        var studentId = TargetUser;
        try
        {
            var response = IsOwn && Session.IsInRole("Student")
                ? await Registrations.GetMyCoursesAsync(page, pageSize, token)
                : await Registrations.GetStudentCoursesAsync(studentId, page, pageSize, token);
            token.ThrowIfCancellationRequested();
            if (disposed || !Allowed || current != requestVersion || studentId != TargetUser) return;
            var data = Data(response);
            hasNext = data.Count == pageSize;
            courses = data.Where(item => item.Status?.Trim().ToLowerInvariant() is not ("rejected" or "cancelled" or "canceled")).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        {
            if (!disposed && current == requestVersion && Allowed && !token.IsCancellationRequested) { error = Error(ex); HandleUnauthorized(ex); }
        }
        finally { if (current == requestVersion) loading = false; }
    }
    private void HandleUnauthorized(Exception ex)
    {
        if (ex is not HttpRequestException { StatusCode: HttpStatusCode.Unauthorized }) return;
        Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true);
    }
    public void Dispose() { disposed = true; requestVersion++; Session.Changed -= SessionChanged; lifetime.Cancel(); requestCancellation?.Dispose(); lifetime.Dispose(); }
}
