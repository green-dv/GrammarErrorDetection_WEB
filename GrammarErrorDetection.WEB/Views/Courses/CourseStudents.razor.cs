using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;

namespace GrammarErrorDetection.WEB.Views.Courses;

public partial class CourseStudents : IDisposable
{
    [Parameter] public Guid ClassId { get; set; }
    [Inject] private ICourseService CourseService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private CourseSearchResponse? course;
    private string? error;
    private bool loading, disposed;
    private readonly CancellationTokenSource lifetime = new();
    private decimal? ExamsMean => course is { Exam1Average: { } first, Exam2Average: { } second } ? (first + second) / 2m : null;
    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        if (disposed || loading) return;
        course = null; error = null;
        if (Session.Current is not { User: { } user }) { Navigation.NavigateTo("/auth/login", replace: true); return; }
        var isTeacher = Session.IsInRole("Teacher");
        if ((!isTeacher && !Session.IsInRole("Coordinator")) || ClassId == Guid.Empty) { error = "No tienes acceso a este curso."; return; }
        loading = true;
        try
        {
            var response = await CourseService.SearchAsync(new()
            {
                PageNumber = 1, PageSize = 1,
                Filter = new() { Id = ClassId.ToString("D"), TeacherId = isTeacher ? user.Id.ToString("D") : null }
            }, lifetime.Token);
            if (disposed) return;
            if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException("No se pudo consultar el curso. Inténtalo de nuevo.");
            var found = response.Data.Count == 1 ? response.Data[0] : null;
            if (found is null || found.Id != ClassId || (isTeacher && found.TeacherId != user.Id))
            {
                error = "El curso no está disponible o no tienes permiso para consultarlo.";
                return;
            }
            course = found;
        }
        catch (HttpRequestException ex)
        {
            if (disposed) return;
            if (ex.StatusCode == HttpStatusCode.Unauthorized) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); }
            error = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Tu sesión venció. Vuelve a iniciar sesión.",
                HttpStatusCode.Forbidden => "No tienes permiso para consultar el curso.",
                HttpStatusCode.NotFound => "No se encontró el curso.",
                _ => "No se pudo cargar el curso. Inténtalo de nuevo."
            };
        }
        catch (OperationCanceledException) { if (!disposed) error = "La consulta tardó demasiado. Inténtalo de nuevo."; }
        catch (JsonException) { if (!disposed) error = "No se pudo interpretar la información del curso."; }
        catch (InvalidOperationException ex) { if (!disposed) error = ex.Message; }
        finally { loading = false; }
    }

    private static string Text(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
    private static string Hour(string? value) => TimeOnly.TryParse(value?.TrimEnd('Z', 'z'), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var time) ? time.ToString("HH:mm") : "—";
    private static string TeacherInitials(string? name) => string.IsNullOrWhiteSpace(name) ? "?" : string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel(); lifetime.Dispose();
    }
}
