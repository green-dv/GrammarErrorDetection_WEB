using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Gamification;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Components;

namespace GrammarErrorDetection.WEB.Views.Gamification;

public partial class GamificationAdmin : IDisposable
{
    [Inject] private IGamificationService Service { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private List<AchievementResponse> achievements = [];
    private List<ChallengeResponse> challenges = [];
    private List<AchievementCollectionResponse> collections = [];
    private Guid? collectionId, appliedCollectionId;
    private bool loadingCollections;
    private string? collectionsError;
    private AchievementResponse? editingAchievement;
    private ChallengeResponse? editingChallenge;
    private AchievementResponse? statusAchievement;
    private bool achievementsTab = true, busy, hasNext, disposed, showEditor;
    private int page = 1, pageSize = 10;
    private string name = "", publication = "", active = "", appliedName = "", appliedPublication = "", appliedActive = "";
    private string? error, message;
    private bool Allowed => Session.Current is not null && (Session.IsInRole("Coordinator") || Session.IsInRole("Teacher") || Session.IsInRole("Student"));
    private bool CanManage => Session.Current is not null && Session.IsInRole("Coordinator");
    private int RowCount => achievementsTab ? achievements.Count : challenges.Count;

    protected override async Task OnInitializedAsync() { if (CheckPermission()) await Task.WhenAll(LoadAsync(1), LoadCollectionsAsync()); }
    private async Task LoadCollectionsAsync()
    {
        if (disposed || loadingCollections || !CheckPermission()) return;
        loadingCollections = true; collectionsError = null;
        try
        {
            var catalog = new Dictionary<Guid, AchievementCollectionResponse>();
            for (var number = 1; ; number++)
            {
                var rows = Require(await Service.SearchAchievementCollectionsAsync(new() { PageNumber = number, PageSize = 100 }, lifetime.Token));
                if (disposed) return;
                foreach (var item in rows)
                    if (item.Id == Guid.Empty || !catalog.TryAdd(item.Id, item))
                        throw new InvalidOperationException("El catálogo de colecciones contiene registros inválidos o páginas repetidas.");
                if (rows.Count < 100) break;
            }
            collections = catalog.Values.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        catch (OperationCanceledException) { if (!disposed) collectionsError = "La consulta de colecciones tardó demasiado."; }
        catch (HttpRequestException ex) { if (!disposed) collectionsError = HandleHttp(ex); }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException)
        { if (!disposed) collectionsError = "No se pudieron cargar las colecciones. Intenta de nuevo."; }
        finally { loadingCollections = false; }
    }
    private bool CheckPermission()
    {
        if (Session.Current is null) { Navigation.NavigateTo("/auth/login", replace: true); return false; }
        return Allowed;
    }
    private async Task SwitchAsync(bool value)
    {
        if (busy || value == achievementsTab) return;
        achievementsTab = value;
        achievements.Clear(); challenges.Clear(); page = 1;
        await ClearAsync();
    }
    private Task ClearAsync() { name = publication = active = ""; collectionId = null; return SearchAsync(); }
    private Task SearchAsync()
    {
        appliedName = name.Trim(); appliedPublication = publication; appliedActive = active;
        appliedCollectionId = collectionId;
        return LoadAsync(1);
    }
    private async Task LoadAsync(int requestedPage)
    {
        if (busy || disposed || !CheckPermission()) return;
        busy = true; error = message = null;
        try
        {
            bool? enabled = bool.TryParse(appliedActive, out var parsed) ? parsed : null;
            if (achievementsTab)
            {
                var rows = Require(await Service.SearchAchievementsAsync(new() { PageNumber = requestedPage, PageSize = pageSize, Active = enabled, Name = appliedName, AchievementCollectionId = appliedCollectionId }, lifetime.Token));
                if (requestedPage > page && rows.Count == 0) { hasNext = false; message = "Has llegado al final del listado."; return; }
                achievements = rows;
            }
            else
            {
                var rows = Require(await Service.SearchChallengesAsync(new() { PageNumber = requestedPage, PageSize = pageSize, Active = enabled, Status = appliedPublication }, lifetime.Token));
                if (requestedPage > page && rows.Count == 0) { hasNext = false; message = "Has llegado al final del listado."; return; }
                challenges = rows;
            }
            page = requestedPage;
            hasNext = RowCount == pageSize;
        }
        catch (OperationCanceledException) { if (!disposed) error = "La consulta tardó demasiado. Intenta de nuevo."; }
        catch (HttpRequestException ex) { if (!disposed) error = HandleHttp(ex); }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException)
        { if (!disposed) error = ex is JsonException ? "No se pudo interpretar la respuesta." : ex.Message; }
        finally { busy = false; if (error is not null) { achievements.Clear(); challenges.Clear(); hasNext = false; } }
    }
    internal static T Require<T>(ApiResponse<T>? response) where T : class
    {
        if (response is null || response.Error is { ErrorCode: not 0 } || response.Data is null)
            throw new InvalidOperationException(response?.Error?.ErrorMessage ?? "La API no devolvió los datos esperados.");
        return response.Data;
    }
    private string HandleHttp(HttpRequestException ex)
    {
        if (ex.StatusCode == HttpStatusCode.Unauthorized) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); return "La sesión expiró."; }
        return ex.StatusCode == HttpStatusCode.Forbidden ? "No tienes permiso para gestionar la gamificación." : "No se pudo consultar la API. Intenta de nuevo.";
    }
    internal static string StatusLabel(string value) => value switch { "Draft" => "Borrador", "Published" => "Publicado", "Closed" => "Cerrado", _ => value };
    internal static string? SafeIcon(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http") ? uri.AbsoluteUri : null;
    private void OpenCreate() { if (!CanManage || !CheckPermission()) return; editingAchievement = null; editingChallenge = null; showEditor = true; }
    private void Edit(AchievementResponse item) { if (!CanManage || !CheckPermission()) return; editingAchievement = item; editingChallenge = null; showEditor = true; }
    private void Edit(ChallengeResponse item) { if (!CanManage || !CheckPermission()) return; editingChallenge = item; editingAchievement = null; showEditor = true; }
    private void CloseEditor() => showEditor = false;
    private void OpenStatus(AchievementResponse item) { if (!busy && CanManage && CheckPermission()) statusAchievement = item; }
    private async Task CloseStatusAsync() { statusAchievement = null; await LoadAsync(page); }
    private async Task StatusChangedAsync()
    {
        statusAchievement = null;
        await LoadAsync(page);
        if (!disposed && error is null && achievements.Count == 0 && page > 1) await LoadAsync(page - 1);
        if (!disposed) message = error is null ? "Estado del logro actualizado." : "El estado se actualizó. No se pudo recargar el listado; pulsa Reintentar.";
    }
    private async Task CloseAndRefreshAsync() { CloseEditor(); await LoadAsync(page); }

    // ---- Repetir (nueva ronda) y Duplicar (copia en borrador) ----
    private ChallengeResponse? repeatingChallenge;
    private void OpenRepeat(ChallengeResponse item) { if (!busy && CanManage && CheckPermission()) repeatingChallenge = item; }
    private async Task RepeatedAsync()
    {
        var round = (repeatingChallenge?.Round ?? 0) + 1;
        repeatingChallenge = null;
        await LoadAsync(page);
        if (!disposed) message = error is null ? $"Se abrió la ronda {round}: el desafío vuelve a estar disponible para todos." : "La ronda se abrió. No se pudo recargar el listado; pulsa Reintentar.";
    }
    private async Task DuplicateAsync(ChallengeResponse item)
    {
        if (busy || !CanManage || !CheckPermission()) return;
        busy = true; error = message = null;
        try
        {
            var copy = Require(await Service.DuplicateChallengeAsync(item.Id, lifetime.Token));
            busy = false;
            await LoadAsync(1);
            if (!disposed && error is null) message = $"Se creó «{copy.Title}» en borrador. Edítalo para ajustar la meta, los puntos o las fechas.";
        }
        catch (HttpRequestException ex) { if (!disposed) error = HandleHttp(ex); }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException) { if (!disposed) error = ex is JsonException ? "No se pudo interpretar la respuesta." : ex.Message; }
        catch (OperationCanceledException) { }
        finally { busy = false; }
    }
    private async Task SavedAsync()
    {
        var isEdit = editingAchievement is not null || editingChallenge is not null;
        CloseEditor();
        await LoadAsync(isEdit ? page : 1);
        if (!disposed) message = error is null ? "Cambios guardados correctamente." : "Los cambios se guardaron. No se pudo actualizar el listado; pulsa Reintentar.";
    }
    public void Dispose() { disposed = true; lifetime.Cancel(); lifetime.Dispose(); }
}
