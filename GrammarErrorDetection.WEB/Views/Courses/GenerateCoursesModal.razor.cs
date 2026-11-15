using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Views.Courses;

public partial class GenerateCoursesModal : IDisposable
{
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<CourseGenerationResponse> OnGenerated { get; set; }
    [Inject] private ICourseService CourseService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CourseGenerationRequest model = new();
    private EditContext editContext = default!;
    private CourseGenerationResponse? result;
    private List<string> validationErrors = [];
    private string? error;
    private bool generating, disposed;
    private readonly CancellationTokenSource lifetime = new();
    private int SourceYear => model.Bimester == 1 ? model.Year - 1 : model.Year;
    private int SourceBimester => model.Bimester == 1 ? 6 : model.Bimester - 1;
    private bool ValidPeriod => model.Year is >= 2 and <= 9999 && model.Bimester is >= 1 and <= 6;

    protected override void OnInitialized()
    {
        var today = DateTime.Today;
        var currentBimester = (today.Month - 1) / 2 + 1;
        model.Year = currentBimester == 6 ? today.Year + 1 : today.Year;
        model.Bimester = currentBimester == 6 ? 1 : currentBimester + 1;
        editContext = new EditContext(model);
    }

    private async Task GenerateAsync()
    {
        if (generating || disposed || result is not null) return;
        error = null;
        validationErrors.Clear();
        if (Session.Current is null) { Navigation.NavigateTo("/auth/login", replace: true); return; }
        if (!Session.IsInRole("Coordinator") || Session.IsInRole("Teacher")) { error = "No tienes permiso para generar cursos."; return; }
        if (!editContext.Validate()) { validationErrors = editContext.GetValidationMessages().Distinct().ToList(); return; }
        generating = true;
        try
        {
            var response = await CourseService.GenerateNextBimesterAsync(model, lifetime.Token);
            if (disposed) return;
            if (response?.Data is not { Generated: true, CreatedClassIds: not null, ReusedClassIds: not null, CreatedRegistrationIds: not null, Skipped: not null } data
                || response.Error is { ErrorCode: not 0 } || data.SourceYear != SourceYear || data.SourceBimester != SourceBimester)
            {
                error = "No se pudo confirmar la generación. Revisa el listado antes de volver a enviar.";
                return;
            }
            result = data;
            await OnGenerated.InvokeAsync(data);
        }
        catch (HttpRequestException ex)
        {
            if (disposed) return;
            if (ex.StatusCode == HttpStatusCode.Unauthorized) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); }
            error = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Tu sesión venció. Vuelve a iniciar sesión.",
                HttpStatusCode.Forbidden => "No tienes permiso para generar cursos.",
                HttpStatusCode.BadRequest => "Revisa el periodo y las fechas de destino.",
                HttpStatusCode.Conflict => "El periodo o los recursos cambiaron. Revisa el listado antes de volver a generar.",
                _ => "No se pudo confirmar la generación. Puede haber cambios ya guardados; revisa el listado antes de volver a enviar."
            };
        }
        catch (OperationCanceledException) { if (!disposed) error = "No se pudo confirmar la generación a tiempo. Revisa el listado antes de volver a enviar."; }
        catch (JsonException) { if (!disposed) error = "No se pudo interpretar el resultado. Revisa el listado antes de volver a generar."; }
        finally { generating = false; }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel(); lifetime.Dispose();
    }
}
