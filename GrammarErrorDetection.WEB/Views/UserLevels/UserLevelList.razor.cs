using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.UserLevels;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Views.UserLevels;

public partial class UserLevelList : IDisposable
{
    [Inject] private IUserLevelCatalogService LevelService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private UserLevelSearchFilter filters = new();
    private UserLevelSearchFilter? appliedFilter;
    private string statusFilter = string.Empty;
    private List<UserLevelItem> levels = [];
    private UserLevelItem? editing, statusLevel;
    private bool creating, busy, updatingStatus, disposed, redirected;
    private int page = 1, pageSize = 10, loadedPageSize = 10, requestVersion;
    private long total;
    private string? error, status, statusError;
    private List<string> validationErrors = [];
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchCancellation;

    private int TotalPages => (int)Math.Max(1, (total + loadedPageSize - 1) / loadedPageSize);
    private string ResultRange => levels.Count == 0
        ? "0 resultados"
        : $"Mostrando {(page - 1) * loadedPageSize + 1}–{(page - 1) * loadedPageSize + levels.Count} de {total}";

    protected override async Task OnInitializedAsync()
    {
        if (CheckSession()) await LoadAsync(1);
    }

    private bool CheckSession()
    {
        if (Session.Current is null)
        {
            if (!redirected) { redirected = true; Navigation.NavigateTo("/auth/login", replace: true); }
            return false;
        }
        if (!Session.IsInRole("Coordinator"))
        {
            error = "La gestión de niveles está disponible para coordinadores.";
            return false;
        }
        return true;
    }

    private async Task SearchAsync()
    {
        appliedFilter = (filters with { Active = bool.TryParse(statusFilter, out var active) ? active : null }).Normalize();
        validationErrors.Clear();
        await LoadAsync(1);
    }

    private void ShowValidationErrors(EditContext context) => validationErrors = context.GetValidationMessages().Distinct().ToList();

    private async Task ClearFiltersAsync()
    {
        filters = new();
        statusFilter = string.Empty;
        appliedFilter = null;
        validationErrors.Clear();
        await LoadAsync(1);
    }

    private Task ChangePageSizeAsync() => LoadAsync(1);
    private Task PreviousPageAsync() => busy || page <= 1 ? Task.CompletedTask : LoadAsync(page - 1);
    private Task NextPageAsync() => busy || page >= TotalPages ? Task.CompletedTask : LoadAsync(page + 1);
    private Task RetryAsync() => LoadAsync(page);

    private async Task LoadAsync(int requestedPage)
    {
        if (disposed || !CheckSession()) return;
        searchCancellation?.Cancel();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        searchCancellation = cancellation;
        var version = ++requestVersion;
        var requestedSize = pageSize;
        busy = true;
        error = null;
        try
        {
            var response = await LevelService.SearchAsync(
                new UserLevelSearchRequest { PageNumber = requestedPage, PageSize = requestedSize, Filter = appliedFilter }, cancellation.Token);
            if (disposed || version != requestVersion) return;
            if (response?.Data is null || response.TotalCount is null || response.Error is { ErrorCode: not 0 })
                throw new InvalidOperationException("No se pudo obtener el listado de niveles. Inténtalo de nuevo.");
            levels = response.Data;
            total = response.TotalCount.Value;
            page = requestedPage;
            loadedPageSize = requestedSize;
            // Si la página pedida quedó vacía (por ejemplo, tras filtrar), vuelve a la última con datos.
            if (levels.Count == 0 && total > 0 && requestedPage > 1)
            {
                busy = false;
                await LoadAsync((int)Math.Max(1, (total + requestedSize - 1) / requestedSize));
            }
        }
        catch (OperationCanceledException)
        {
            if (!disposed && version == requestVersion && !cancellation.IsCancellationRequested) error = "La consulta tardó demasiado. Inténtalo de nuevo.";
        }
        catch (HttpRequestException ex) { if (!disposed && version == requestVersion) error = HttpError(ex); }
        catch (JsonException) { if (!disposed && version == requestVersion) error = "No se pudo interpretar el listado recibido."; }
        catch (InvalidOperationException ex) { if (!disposed && version == requestVersion) error = ex.Message; }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex) { if (!disposed && version == requestVersion) error = ex.Message; }
        finally
        {
            if (version == requestVersion)
            {
                busy = false;
                if (error is not null)
                {
                    levels = [];
                    total = 0;
                    page = requestedPage;
                }
                searchCancellation = null;
            }
            cancellation.Dispose();
        }
    }

    private string HttpError(HttpRequestException exception)
    {
        if (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            Session.Clear();
            CheckSession();
            return "Tu sesión venció. Vuelve a iniciar sesión.";
        }
        return exception.StatusCode switch
        {
            HttpStatusCode.Forbidden => "No tienes permiso para consultar los niveles.",
            HttpStatusCode.BadRequest => "Revisa los filtros e inténtalo de nuevo.",
            HttpStatusCode.TooManyRequests => "Se realizaron demasiadas consultas. Espera un momento e inténtalo de nuevo.",
            _ => "No se pudo conectar con el servicio de niveles. Inténtalo de nuevo."
        };
    }

    private async Task CreatedAsync(UserLevelItem level)
    {
        creating = false;
        status = $"Nivel «{level.Name}» creado.";
        // El listado se ordena por número de nivel: se recarga desde la primera página.
        await LoadAsync(1);
    }

    private async Task EditedAsync(UserLevelItem level)
    {
        editing = null;
        status = $"Nivel «{level.Name}» actualizado.";
        await LoadAsync(page);
    }

    private void CloseStatus()
    {
        if (updatingStatus) return;
        statusLevel = null;
        statusError = null;
    }

    private async Task ConfirmStatusAsync()
    {
        if (statusLevel is not { } level || updatingStatus || !CheckSession()) return;
        updatingStatus = true;
        statusError = null;
        var disable = level.Active;
        try
        {
            var response = await LevelService.UpdateAsync(new UserLevelUpdateRequest { Id = level.Id, Active = !level.Active }, lifetime.Token);
            if (disposed) return;
            if (response?.Data is not { } updated || updated.Id != level.Id || response.Error is { ErrorCode: not 0 })
            {
                statusError = "No se pudo confirmar el cambio de estado. Inténtalo de nuevo.";
                return;
            }
            statusLevel = null;
            status = disable ? $"Nivel «{updated.Name}» deshabilitado." : $"Nivel «{updated.Name}» reactivado.";
            await LoadAsync(page);
        }
        catch (HttpRequestException ex)
        {
            if (disposed) return;
            statusError = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Tu sesión venció. Vuelve a iniciar sesión.",
                HttpStatusCode.Forbidden => "Solo un coordinador puede cambiar el estado de un nivel.",
                HttpStatusCode.NotFound => "El nivel ya no existe. Actualiza el listado.",
                HttpStatusCode.BadRequest => "No se pudo cambiar el estado: debe quedar siempre un nivel activo con 0 puntos y los puntos mínimos deben aumentar con el nivel.",
                _ => "No se pudo cambiar el estado. Inténtalo de nuevo."
            };
            if (ex.StatusCode == HttpStatusCode.Unauthorized) { Session.Clear(); CheckSession(); }
        }
        catch (OperationCanceledException) { if (!disposed) statusError = "El cambio de estado tardó demasiado. Inténtalo de nuevo."; }
        catch (JsonException) { if (!disposed) statusError = "No se pudo interpretar la respuesta del servidor."; }
        finally { updatingStatus = false; }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        searchCancellation?.Cancel();
        lifetime.Dispose();
    }
}
