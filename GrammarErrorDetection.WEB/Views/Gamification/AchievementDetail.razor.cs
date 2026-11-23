using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Gamification;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;

namespace GrammarErrorDetection.WEB.Views.Gamification;

public partial class AchievementDetail : IDisposable
{
    [Parameter] public Guid AchievementId { get; set; }
    [Inject] private IGamificationService Service { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private AchievementResponse? achievement;
    private bool loading, disposed, showEdit, showStatus;
    private string? error, message;
    private bool Allowed => Session.Current is not null && (Session.IsInRole("Coordinator") || Session.IsInRole("Teacher") || Session.IsInRole("Student"));
    private bool CanManage => Session.Current is not null && Session.IsInRole("Coordinator");
    protected override async Task OnInitializedAsync() => await LoadAsync();
    private async Task LoadAsync()
    {
        if (disposed || loading) return;
        if (Session.Current is null) { Navigation.NavigateTo("/auth/login", replace: true); return; }
        if (!Allowed) return;
        loading = true; error = message = null;
        try
        {
            var rows = GamificationAdmin.Require(await Service.SearchAchievementsAsync(new() { Id = AchievementId, PageSize = 1 }, lifetime.Token));
            if (disposed) return;
            if (rows.Count != 1 || rows[0].Id != AchievementId) throw new InvalidOperationException(I18n.T("No se encontró el logro solicitado."));
            achievement = rows[0];
        }
        catch (OperationCanceledException) { if (!disposed) error = I18n.T("La consulta tardó demasiado."); }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); }
            if (!disposed) error = ex.StatusCode == HttpStatusCode.Forbidden ? I18n.T("No tienes permiso para consultar este logro.") : ApiErrors.Describe(ex);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException or ArgumentException)
        { if (!disposed) error = ex is InvalidOperationException ? ex.Message : I18n.T("No se pudo interpretar el logro."); }
        finally { loading = false; if (error is not null) achievement = null; }
    }
    private void OpenEdit() { if (!loading && CanManage) showEdit = true; }
    private void OpenStatus() { if (!loading && CanManage) showStatus = true; }
    private async Task CloseDialogsAsync() { showEdit = showStatus = false; await LoadAsync(); }
    private async Task SavedAsync()
    {
        await CloseDialogsAsync();
        if (!disposed) message = error is null ? I18n.T("Cambios guardados correctamente.") : I18n.T("Los cambios se guardaron. No se pudo actualizar la vista; pulsa Reintentar.");
    }
    public void Dispose() { disposed = true; lifetime.Cancel(); lifetime.Dispose(); }
}
