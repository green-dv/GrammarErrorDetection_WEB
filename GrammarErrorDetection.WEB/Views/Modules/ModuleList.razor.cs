using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Modules;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Views.Modules;

public partial class ModuleList : IDisposable
{
    [Inject] private IModuleCatalogService ModuleService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private ModuleSearchFilter filters = new();
    private ModuleSearchFilter? appliedFilter;
    private string statusFilter = string.Empty;
    private List<ModuleItem> modules = [];
    private ModuleItem? editing, statusModule;
    private bool creating, busy, updatingStatus, disposed, redirected;
    private int page = 1, pageSize = 10, loadedPageSize = 10, requestVersion;
    private long total;
    private string? error, status, statusError;
    private List<string> validationErrors = [];
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchCancellation;

    private int TotalPages => (int)Math.Max(1, (total + loadedPageSize - 1) / loadedPageSize);
    private string ResultRange => modules.Count == 0
        ? "0 resultados"
        : I18n.T("Mostrando {0}–{1} de {2}", (page - 1) * loadedPageSize + 1, (page - 1) * loadedPageSize + modules.Count, total);

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
            error = I18n.T("La gestión de módulos está disponible para coordinadores.");
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
            var response = await ModuleService.SearchAsync(
                new ModuleSearchRequest { PageNumber = requestedPage, PageSize = requestedSize, Filter = appliedFilter }, cancellation.Token);
            if (disposed || version != requestVersion) return;
            if (response?.Data is null || response.TotalCount is null || response.Error is { ErrorCode: not 0 })
                throw new InvalidOperationException(I18n.T("No se pudo obtener el listado de módulos. Inténtalo de nuevo."));
            modules = response.Data;
            total = response.TotalCount.Value;
            page = requestedPage;
            loadedPageSize = requestedSize;
            // Si la página pedida quedó vacía (por ejemplo, tras filtrar), vuelve a la última con datos.
            if (modules.Count == 0 && total > 0 && requestedPage > 1)
            {
                busy = false;
                await LoadAsync((int)Math.Max(1, (total + requestedSize - 1) / requestedSize));
            }
        }
        catch (OperationCanceledException)
        {
            if (!disposed && version == requestVersion && !cancellation.IsCancellationRequested) error = I18n.T("La consulta tardó demasiado. Inténtalo de nuevo.");
        }
        catch (HttpRequestException ex) { if (!disposed && version == requestVersion) error = HttpError(ex); }
        catch (JsonException) { if (!disposed && version == requestVersion) error = I18n.T("No se pudo interpretar el listado recibido."); }
        catch (InvalidOperationException ex) { if (!disposed && version == requestVersion) error = ex.Message; }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex) { if (!disposed && version == requestVersion) error = ex.Message; }
        finally
        {
            if (version == requestVersion)
            {
                busy = false;
                if (error is not null)
                {
                    modules = [];
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
            return I18n.T("Tu sesión venció. Vuelve a iniciar sesión.");
        }
        return exception.StatusCode switch
        {
            HttpStatusCode.Forbidden => I18n.T("No tienes permiso para consultar los módulos."),
            HttpStatusCode.BadRequest => I18n.T("Revisa los filtros e inténtalo de nuevo."),
            HttpStatusCode.TooManyRequests => I18n.T("Se realizaron demasiadas consultas. Espera un momento e inténtalo de nuevo."),
            _ => I18n.T("No se pudo conectar con el servicio de módulos. Inténtalo de nuevo.")
        };
    }

    private async Task CreatedAsync(ModuleItem module)
    {
        creating = false;
        status = I18n.T("Módulo «{0}» creado.", module.Name);
        // El listado se ordena por nombre: se recarga desde la primera página para ubicar el nuevo registro.
        await LoadAsync(1);
    }

    private async Task EditedAsync(ModuleItem module)
    {
        editing = null;
        status = I18n.T("Módulo «{0}» actualizado.", module.Name);
        await LoadAsync(page);
    }

    private void CloseStatus()
    {
        if (updatingStatus) return;
        statusModule = null;
        statusError = null;
    }

    private async Task ConfirmStatusAsync()
    {
        if (statusModule is not { } module || updatingStatus || !CheckSession()) return;
        updatingStatus = true;
        statusError = null;
        var disable = module.Active;
        try
        {
            var response = await ModuleService.UpdateAsync(new ModuleUpdateRequest { Id = module.Id, Active = !module.Active }, lifetime.Token);
            if (disposed) return;
            if (response?.Data is not { } updated || updated.Id != module.Id || response.Error is { ErrorCode: not 0 })
            {
                statusError = I18n.T("No se pudo confirmar el cambio de estado. Inténtalo de nuevo.");
                return;
            }
            statusModule = null;
            status = disable ? I18n.T("Módulo «{0}» deshabilitado.", updated.Name) : I18n.T("Módulo «{0}» reactivado.", updated.Name);
            await LoadAsync(page);
        }
        catch (HttpRequestException ex)
        {
            if (disposed) return;
            statusError = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => I18n.T("Tu sesión venció. Vuelve a iniciar sesión."),
                HttpStatusCode.Forbidden => I18n.T("Solo un coordinador puede cambiar el estado de un módulo."),
                HttpStatusCode.NotFound => I18n.T("El módulo ya no existe. Actualiza el listado."),
                _ => I18n.T("No se pudo cambiar el estado. Inténtalo de nuevo.")
            };
            if (ex.StatusCode == HttpStatusCode.Unauthorized) { Session.Clear(); CheckSession(); }
        }
        catch (OperationCanceledException) { if (!disposed) statusError = I18n.T("El cambio de estado tardó demasiado. Inténtalo de nuevo."); }
        catch (JsonException) { if (!disposed) statusError = I18n.T("No se pudo interpretar la respuesta del servidor."); }
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
