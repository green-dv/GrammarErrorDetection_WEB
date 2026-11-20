using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Categories;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Views.Categories;

public partial class CategoryList : IDisposable
{
    [Inject] private IErrorCategoryService CategoryService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private ErrorCategorySearchFilter filters = new();
    private ErrorCategorySearchFilter? appliedFilter;
    private string statusFilter = string.Empty;
    private List<ErrorCategory> categories = [];
    private ErrorCategory? editing, statusCategory;
    private bool creating, busy, updatingStatus, disposed, redirected;
    private int page = 1, pageSize = 10, loadedPageSize = 10, requestVersion;
    private long total;
    private string? error, status, statusError;
    private List<string> validationErrors = [];
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchCancellation;

    private int TotalPages => (int)Math.Max(1, (total + loadedPageSize - 1) / loadedPageSize);
    private string ResultRange => categories.Count == 0
        ? "0 resultados"
        : I18n.T("Mostrando {0}–{1} de {2}", (page - 1) * loadedPageSize + 1, (page - 1) * loadedPageSize + categories.Count, total);

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
            error = I18n.T("La gestión de categorías está disponible para coordinadores.");
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
            var response = await CategoryService.SearchAsync(
                new ErrorCategorySearchRequest { PageNumber = requestedPage, PageSize = requestedSize, Filter = appliedFilter }, cancellation.Token);
            if (disposed || version != requestVersion) return;
            if (response?.Data is null || response.TotalCount is null || response.Error is { ErrorCode: not 0 })
                throw new InvalidOperationException(I18n.T("No se pudo obtener el listado de categorías. Inténtalo de nuevo."));
            categories = response.Data;
            total = response.TotalCount.Value;
            page = requestedPage;
            loadedPageSize = requestedSize;
            // Si se eliminó la última fila de la última página, retrocede a la última página con datos.
            if (categories.Count == 0 && total > 0 && requestedPage > 1)
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
                    categories = [];
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
            HttpStatusCode.Forbidden => I18n.T("No tienes permiso para consultar estas categorías."),
            HttpStatusCode.BadRequest => I18n.T("Revisa los filtros e inténtalo de nuevo."),
            HttpStatusCode.TooManyRequests => I18n.T("Se realizaron demasiadas consultas. Espera un momento e inténtalo de nuevo."),
            _ => I18n.T("No se pudo conectar con el servicio de categorías. Inténtalo de nuevo.")
        };
    }

    private async Task CreatedAsync(ErrorCategory category)
    {
        creating = false;
        status = I18n.T("Categoría «{0}» creada.", category.Name);
        // La más reciente aparece primero, por eso se vuelve a la primera página.
        await LoadAsync(1);
    }

    private async Task EditedAsync(ErrorCategory category)
    {
        editing = null;
        status = I18n.T("Categoría «{0}» actualizada.", category.Name);
        await LoadAsync(page);
    }

    private void CloseStatus()
    {
        if (updatingStatus) return;
        statusCategory = null;
        statusError = null;
    }

    private async Task ConfirmStatusAsync()
    {
        if (statusCategory is not { } category || updatingStatus || !CheckSession()) return;
        updatingStatus = true;
        statusError = null;
        var disable = category.Active;
        try
        {
            var response = await CategoryService.UpdateAsync(new ErrorCategoryUpdateRequest { Id = category.Id, Active = !category.Active }, lifetime.Token);
            if (disposed) return;
            if (response?.Data is not { } updated || updated.Id != category.Id || response.Error is { ErrorCode: not 0 })
            {
                statusError = I18n.T("No se pudo confirmar el cambio de estado. Inténtalo de nuevo.");
                return;
            }
            statusCategory = null;
            status = disable ? I18n.T("Categoría «{0}» deshabilitada.", updated.Name) : I18n.T("Categoría «{0}» reactivada.", updated.Name);
            await LoadAsync(page);
        }
        catch (HttpRequestException ex)
        {
            if (disposed) return;
            statusError = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => I18n.T("Tu sesión venció. Vuelve a iniciar sesión."),
                HttpStatusCode.Forbidden => I18n.T("Solo un coordinador puede cambiar el estado de una categoría."),
                HttpStatusCode.NotFound => I18n.T("La categoría ya no existe. Actualiza el listado."),
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
