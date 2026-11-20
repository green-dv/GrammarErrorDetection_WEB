using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Points;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;

namespace GrammarErrorDetection.WEB.Views.Points;

public partial class PointsDashboard : IDisposable
{
    [Inject] private IPointService Points { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private bool loading, disposed;
    private string? error;
    private PointTotalResponse? total;
    private LevelProgress? progress;
    protected override Task OnInitializedAsync() => Task.WhenAll(LoadAsync(), LoadHistoryAsync(1));
    private List<PointTransactionResponse> history = [];
    private int page = 1, pageSize = 10;
    private bool historyLoading, hasNext;
    private string? historyError, historyStatus;
    private long? historyTotal;
    private string HistoryRange => history.Count == 0 ? "0 movimientos" : I18n.T("Mostrando {0}–{1}", (page - 1) * pageSize + 1, (page - 1) * pageSize + history.Count) + (historyTotal is { } all ? I18n.T(" de {0}", all) : "");
    private string PageOf => historyTotal is { } all ? I18n.T(" de {0}", Math.Max(1, (all + pageSize - 1) / pageSize)) : "";
    private Task ChangeHistorySizeAsync() => LoadHistoryAsync(1);
    private Task PreviousHistoryAsync() => page <= 1 ? Task.CompletedTask : LoadHistoryAsync(page - 1);
    private Task NextHistoryAsync() => !hasNext ? Task.CompletedTask : LoadHistoryAsync(page + 1);
    private Task RetryHistoryAsync() => LoadHistoryAsync(page);
    private async Task LoadHistoryAsync(int requestedPage)
    {
        if (historyLoading || disposed || Session.Current?.User is not { } user) return;
        historyLoading = true; historyError = historyStatus = null;
        try
        {
            var (entries, total) = await Points.GetHistoryPageAsync(user.Id, requestedPage, pageSize, lifetime.Token);
            if (disposed) return;
            if (requestedPage > page && entries.Count == 0)
            {
                hasNext = false;
                historyStatus = I18n.T("Has llegado al final del historial.");
                return;
            }
            history = entries;
            historyTotal = total;
            page = requestedPage;
            hasNext = total is { } all ? (long)requestedPage * pageSize < all : entries.Count == pageSize;
        }
        catch (HttpRequestException ex)
        {
            if (disposed) return;
            if (ex.StatusCode == HttpStatusCode.Unauthorized) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); }
            historyError = ex.StatusCode == HttpStatusCode.Forbidden ? I18n.T("No tienes permiso para consultar este historial.") : I18n.T("No se pudo consultar el historial. Inténtalo de nuevo.");
        }
        catch (OperationCanceledException) { if (!disposed) historyError = I18n.T("La consulta del historial tardó demasiado. Inténtalo de nuevo."); }
        catch (JsonException) { if (!disposed) historyError = I18n.T("No se pudo interpretar el historial de puntos."); }
        catch (InvalidOperationException ex) { if (!disposed) historyError = ex.Message; }
        finally
        {
            historyLoading = false;
            if (historyError is not null) { history = []; historyTotal = null; page = requestedPage; hasNext = false; }
        }
    }
    private async Task LoadAsync()
    {
        if (loading || disposed || Session.Current?.User is not { } user) return;
        loading = true; error = null; total = null; progress = null;
        try
        {
            total = await Points.GetTotalAsync(user.Id, cancellationToken: lifetime.Token);
            if (disposed) return;
            var levels = await Points.GetLevelsAsync(lifetime.Token);
            if (disposed) return;
            if (levels.Count == 0) throw new InvalidOperationException(I18n.T("Aún no hay niveles activos configurados."));
            progress = LevelProgress.From(total.TotalPoints, levels);
        }
        catch (HttpRequestException ex)
        {
            if (disposed) return;
            if (ex.StatusCode == HttpStatusCode.Unauthorized) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); }
            error = ex.StatusCode == HttpStatusCode.Forbidden ? I18n.T("No tienes permiso para consultar tu progreso.") : I18n.T("No se pudo consultar tu progreso. Inténtalo de nuevo.");
        }
        catch (OperationCanceledException) { if (!disposed) error = I18n.T("La consulta tardó demasiado. Inténtalo de nuevo."); }
        catch (JsonException) { if (!disposed) error = I18n.T("No se pudo interpretar tu progreso."); }
        catch (InvalidOperationException ex) { if (!disposed) error = ex.Message; }
        finally { loading = false; }
    }
    public void Dispose() { disposed = true; lifetime.Cancel(); lifetime.Dispose(); }
}
