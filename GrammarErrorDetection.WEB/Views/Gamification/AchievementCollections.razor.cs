using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Gamification;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;

namespace GrammarErrorDetection.WEB.Views.Gamification;

public partial class AchievementCollections : IDisposable
{
    [Parameter] public Guid? CollectionId { get; set; }
    [Inject] private IGamificationService Service { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private List<AchievementCollectionResponse> collections = [];
    private List<AchievementResponse> achievements = [];
    private AchievementCollectionResponse? collection;
    private AchievementCollectionResponse? editingCollection, statusCollection;
    private AchievementResponse? editingAchievement, statusAchievement;
    private bool showAchievementEditor;
    private bool loading, disposed, hasNext, showCreate;
    private const int PageSize = 10;
    private int page = 1;
    private string name = "", active = "", appliedName = "", appliedActive = "";
    private string? error, message;
    private bool Allowed => Session.Current is not null && (Session.IsInRole("Coordinator") || Session.IsInRole("Teacher") || Session.IsInRole("Student"));
    private bool CanManage => Session.Current is not null && Session.IsInRole("Coordinator");
    private bool IsDetail => CollectionId.HasValue;
    private int RowCount => IsDetail ? achievements.Count : collections.Count;
    protected override async Task OnInitializedAsync() => await LoadAsync(1);
    private bool CheckPermission()
    {
        if (Session.Current is null) { Navigation.NavigateTo("/auth/login", replace: true); return false; }
        return Allowed;
    }
    private Task SearchAsync() { appliedName = name.Trim(); appliedActive = active; return LoadAsync(1); }
    private Task ClearAsync() { name = active = ""; return SearchAsync(); }
    private async Task LoadAsync(int number)
    {
        if (loading || disposed || !CheckPermission()) return;
        loading = true; error = message = null;
        try
        {
            if (CollectionId is { } id)
            {
                if (collection is null)
                {
                    var rows = GamificationAdmin.Require(await Service.SearchAchievementCollectionsAsync(new() { Id = id, PageSize = 1 }, lifetime.Token));
                    if (rows.Count != 1 || rows[0].Id != id) throw new InvalidOperationException("No se encontró la colección solicitada.");
                    collection = rows[0];
                }
                var rowsOfAchievements = GamificationAdmin.Require(await Service.SearchAchievementsAsync(new()
                {
                    AchievementCollectionId = id, PageNumber = number, PageSize = PageSize
                }, lifetime.Token));
                if (disposed) return;
                if (rowsOfAchievements.Any(item => item.AchievementCollectionId != id))
                    throw new InvalidOperationException("Los logros recibidos no corresponden a esta colección.");
                if (number > page && rowsOfAchievements.Count == 0) { hasNext = false; message = "Has llegado al final de los logros."; return; }
                achievements = rowsOfAchievements;
            }
            else
            {
                var rows = GamificationAdmin.Require(await Service.SearchAchievementCollectionsAsync(new()
                {
                    Name = appliedName, Active = bool.TryParse(appliedActive, out var enabled) ? enabled : null,
                    PageNumber = number, PageSize = PageSize
                }, lifetime.Token));
                if (disposed) return;
                if (number > page && rows.Count == 0) { hasNext = false; message = "Has llegado al final de las colecciones."; return; }
                collections = rows;
            }
            page = number;
            hasNext = RowCount == PageSize;
        }
        catch (OperationCanceledException) { if (!disposed) error = "La consulta tardó demasiado. Intenta de nuevo."; }
        catch (HttpRequestException ex)
        {
            if (!disposed)
            {
                if (ex.StatusCode == HttpStatusCode.Unauthorized) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); }
                error = ex.StatusCode == HttpStatusCode.Forbidden ? "No tienes permiso para consultar las colecciones." : "No se pudo consultar el servidor. Intenta de nuevo.";
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException or ArgumentException)
        { if (!disposed) error = ex is InvalidOperationException ? ex.Message : "No se pudo interpretar la colección o sus logros."; }
        finally
        {
            loading = false;
            if (error is not null) { collections = []; achievements = []; hasNext = false; }
        }
    }
    private void OpenCreate() { if (!loading && CanManage && CheckPermission()) { editingCollection = null; showCreate = true; } }
    private void OpenEditCollection(AchievementCollectionResponse item) { if (!loading && CanManage && CheckPermission()) { editingCollection = item; showCreate = true; } }
    private void OpenEditAchievement(AchievementResponse? item) { if (!loading && CanManage && CheckPermission()) { editingAchievement = item; showAchievementEditor = true; } }
    private void OpenStatus(AchievementCollectionResponse item) { if (!loading && CanManage && CheckPermission()) statusCollection = item; }
    private void OpenStatus(AchievementResponse item) { if (!loading && CanManage && CheckPermission()) statusAchievement = item; }
    private void CloseCreate() { showCreate = false; editingCollection = null; }
    private void CloseDialogs() { CloseCreate(); showAchievementEditor = false; editingAchievement = statusAchievement = null; statusCollection = null; }
    private async Task CloseAndReloadAsync() { CloseDialogs(); if (IsDetail) collection = null; await LoadAsync(page); }
    private async Task ChangesSavedAsync()
    {
        await CloseAndReloadAsync();
        if (!disposed && error is null && RowCount == 0 && page > 1) await LoadAsync(page - 1);
        if (!disposed) message = error is null ? "Cambios guardados correctamente." : "Los cambios se guardaron. No se pudo actualizar la vista; pulsa Reintentar.";
    }
    private Task CollectionSavedAsync(AchievementCollectionResponse _) => ChangesSavedAsync();
    private async Task CreatedAsync(AchievementCollectionResponse result)
    {
        CloseCreate();
        name = active = appliedName = appliedActive = "";
        await LoadAsync(1);
        if (!disposed) message = error is null ? $"Colección «{result.Name}» registrada correctamente."
            : $"La colección «{result.Name}» se registró. No se pudo actualizar el listado; pulsa Reintentar.";
    }
    public void Dispose() { disposed = true; lifetime.Cancel(); lifetime.Dispose(); }
}
