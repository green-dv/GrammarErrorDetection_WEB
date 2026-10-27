using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Materials;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.WEB.Views.Materials;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace GrammarErrorDetection.WEB.Pages;

public partial class Materials : IDisposable
{
    [Inject] private ISupportMaterialService MaterialService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchCancellation;
    private string name = "", categoryId = "", fileTypeId = "", activeFilter = "true";
    private string? error, validationError, status, dialogMode, dialogError, categoryError, fileTypeError;
    private List<SupportMaterialResponse> materials = [];
    private List<ErrorClassificationResponse> categories = [];
    private List<FileTypeResponse> fileTypes = [];
    private readonly Dictionary<Guid, string> knownCategoryNames = [];
    private readonly Dictionary<Guid, FileTypeResponse> knownFileTypes = [];
    private SupportMaterialFilter appliedFilter = new() { Active = true };
    private SupportMaterialResponse? selected;
    private MaterialEditForm? editForm;
    private bool loading, loadingDetail, loadingCategories, loadingFileTypes, saving, hasNext, disposed;
    private int page = 1, pageSize = 10, loadedPageSize = 10, requestVersion;
    private bool CanManage => Session.IsInRole("Teacher") || Session.IsInRole("Coordinator");
    private bool ActionBusy => saving || loadingDetail;
    private string ApiBaseUrl => Configuration["Api:BaseUrl"]!;
    private string DialogTitle => dialogMode switch
    {
        "create" => "Agregar material", "edit" => "Editar material",
        "status" => selected?.Active == true ? "Desactivar material" : "Reactivar material",
        _ => "Material de apoyo"
    };
    private string ResultRange => materials.Count == 0 ? "0 resultados"
        : $"Mostrando {(page - 1) * loadedPageSize + 1}–{(page - 1) * loadedPageSize + materials.Count}";
    private bool HasChanges => editForm is not null && (dialogMode == "create" || selected is not null && (
        editForm.Name.Trim() != selected.Name || editForm.FileLink.Trim() != selected.FileLink
        || !Guid.TryParse(editForm.ErrorClassificationId, out var category) || category != selected.ErrorClassificationId
        || !Guid.TryParse(editForm.FileTypeId, out var type) || type != selected.FileTypeId
        || editForm.UploadDate != selected.UploadDate || editForm.FileSize is not null && editForm.FileSize != selected.FileSize));

    protected override Task OnInitializedAsync() => Task.WhenAll(LoadAsync(1), LoadCategoriesAsync(), LoadFileTypesAsync());

    private async Task LoadCategoriesAsync()
    {
        if (disposed || loadingCategories || !CheckSession()) return;
        loadingCategories = true;
        categoryError = null;
        try
        {
            var rows = await ReadCatalogAsync(MaterialService.GetClassificationsAsync, item => item.Id, item => item.Active);
            if (disposed) return;
            categories = rows.OrderBy(item => item.Name).ToList();
            foreach (var item in rows) knownCategoryNames[item.Id] = item.Name;
        }
        catch (OperationCanceledException) { if (!disposed) categoryError = "La consulta de clasificaciones tardó demasiado."; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed) categoryError = "No se pudieron cargar las clasificaciones. " + ErrorMessage(ex); }
        finally { loadingCategories = false; }
    }

    private async Task LoadFileTypesAsync()
    {
        if (disposed || loadingFileTypes || !CheckSession()) return;
        loadingFileTypes = true;
        fileTypeError = null;
        try
        {
            var rows = await ReadCatalogAsync(MaterialService.GetFileTypesAsync, item => item.Id, item => item.Active);
            if (disposed) return;
            fileTypes = rows.OrderBy(item => item.Name).ToList();
            foreach (var item in rows) knownFileTypes[item.Id] = item;
        }
        catch (OperationCanceledException) { if (!disposed) fileTypeError = "La consulta de tipos de archivo tardó demasiado."; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed) fileTypeError = "No se pudieron cargar los tipos de archivo. " + ErrorMessage(ex); }
        finally { loadingFileTypes = false; }
    }

    private async Task<List<T>> ReadCatalogAsync<T>(Func<int, CancellationToken, Task<ApiResponse<List<T>>?>> getPage, Func<T, Guid> id, Func<T, bool> active)
    {
        Dictionary<Guid, T> rows = [];
        for (var number = 1; ; number++)
        {
            var batch = RequireData(await getPage(number, lifetime.Token));
            foreach (var item in batch)
            {
                if (id(item) == Guid.Empty || !active(item) || !rows.TryAdd(id(item), item))
                    throw new InvalidOperationException("El catálogo contiene registros inválidos o páginas repetidas. Inténtalo de nuevo.");
            }
            if (batch.Count < 100) return rows.Values.ToList();
        }
    }

    private bool CheckSession(bool manage = false)
    {
        if (Session.Current?.User is null)
        {
            Navigation.NavigateTo("/auth/login", replace: true);
            return false;
        }
        if (manage && !CanManage) { dialogError = "Solo docentes y coordinadores pueden modificar el material."; return false; }
        return true;
    }

    private Task SearchAsync()
    {
        if (loading || ActionBusy) return Task.CompletedTask;
        validationError = status = null;
        Guid? category = null, type = null;
        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            if (!Guid.TryParse(categoryId, out var id) || id == Guid.Empty)
            { validationError = "Selecciona una clasificación válida o deja el filtro vacío."; return Task.CompletedTask; }
            category = id;
        }
        if (!string.IsNullOrWhiteSpace(fileTypeId))
        {
            if (!Guid.TryParse(fileTypeId, out var id) || id == Guid.Empty)
            { validationError = "Selecciona un tipo de archivo válido o deja el filtro vacío."; return Task.CompletedTask; }
            type = id;
        }
        appliedFilter = new()
        {
            Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            ErrorClassificationId = category, FileTypeId = type,
            Active = CanManage ? bool.TryParse(activeFilter, out var active) ? active : null : true
        };
        return LoadAsync(1);
    }

    private Task ClearFiltersAsync()
    {
        if (loading || ActionBusy) return Task.CompletedTask;
        name = categoryId = fileTypeId = "";
        activeFilter = "true";
        return SearchAsync();
    }
    private Task RetryAsync() => loading || ActionBusy ? Task.CompletedTask : LoadAsync(page);
    private Task ChangePageSizeAsync() => ActionBusy ? Task.CompletedTask : LoadAsync(1);
    private Task PreviousPageAsync() => loading || ActionBusy || page <= 1 ? Task.CompletedTask : LoadAsync(page - 1);
    private Task NextPageAsync() => loading || ActionBusy || !hasNext ? Task.CompletedTask : LoadAsync(page + 1);

    private async Task LoadAsync(int requestedPage)
    {
        if (disposed || !CheckSession()) return;
        searchCancellation?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        searchCancellation = cancellation;
        var version = ++requestVersion;
        var requestedSize = pageSize;
        var filter = appliedFilter;
        loading = true;
        error = null;
        try
        {
            var response = await MaterialService.SearchAsync(new() { PageNumber = requestedPage, PageSize = requestedSize, Filter = filter }, cancellation.Token);
            if (disposed || version != requestVersion) return;
            var rows = RequireData(response);
            if (rows.Any(item => item.Id == Guid.Empty || filter.Active is { } active && item.Active != active))
                throw new InvalidOperationException("La respuesta no corresponde a los materiales solicitados.");
            // Count es el número de registros de esta página, no el total global.
            if (requestedPage > page && rows.Count == 0) { hasNext = false; return; }
            materials = rows;
            page = requestedPage;
            loadedPageSize = requestedSize;
            hasNext = rows.Count == requestedSize;
            foreach (var material in rows) RememberOptions(material);
        }
        catch (OperationCanceledException)
        { if (!disposed && version == requestVersion && !cancellation.IsCancellationRequested) error = "La consulta tardó demasiado. Inténtalo de nuevo."; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed && version == requestVersion) error = ErrorMessage(ex); }
        finally
        {
            if (version == requestVersion)
            {
                loading = false;
                searchCancellation = null;
                if (error is not null) { materials = []; hasNext = false; }
            }
        }
    }

    private void OpenCreate()
    {
        if (ActionBusy || !CheckSession(manage: true)) return;
        selected = null;
        editForm = new();
        dialogError = null;
        dialogMode = "create";
    }

    private async Task OpenDetailAsync(SupportMaterialResponse material, bool edit)
    {
        if (ActionBusy || !CheckSession(edit)) return;
        selected = null; editForm = null; dialogError = null;
        dialogMode = edit ? "edit" : "preview";
        loadingDetail = true;
        try
        {
            var detail = RequireData(await MaterialService.GetAsync(material.Id, lifetime.Token));
            if (disposed) return;
            if (detail.Id != material.Id) throw new InvalidOperationException("No se recibió el material solicitado.");
            selected = detail;
            RememberOptions(detail);
            if (edit) editForm = MaterialEditForm.From(detail);
        }
        catch (OperationCanceledException) { if (!disposed) dialogError = "La consulta del material tardó demasiado. Cierra la ventana e inténtalo de nuevo."; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed) dialogError = ErrorMessage(ex); }
        finally { loadingDetail = false; }
    }

    private void OpenStatus(SupportMaterialResponse material)
    {
        if (ActionBusy || !CheckSession(manage: true)) return;
        selected = material; editForm = null; dialogError = null; dialogMode = "status";
    }

    private void CloseDialog()
    {
        if (ActionBusy) return;
        dialogMode = null; selected = null; editForm = null; dialogError = null;
    }

    private async Task SaveAsync()
    {
        if (disposed || ActionBusy || loadingCategories || loadingFileTypes || editForm is null || !CheckSession(manage: true)
            || dialogMode is not ("create" or "edit")) return;
        var errors = editForm.Validate(ApiBaseUrl);
        if (!categories.Any(item => item.Id.ToString("D") == editForm.ErrorClassificationId)
            && selected?.ErrorClassificationId.ToString("D") != editForm.ErrorClassificationId)
            errors.Add("Selecciona una clasificación disponible.");
        if (!fileTypes.Any(item => item.Id.ToString("D") == editForm.FileTypeId)
            && selected?.FileTypeId.ToString("D") != editForm.FileTypeId)
            errors.Add("Selecciona un tipo de archivo disponible.");
        if (errors.Count > 0) { dialogError = string.Join(" ", errors); return; }
        if (!HasChanges) return;
        var creating = dialogMode == "create";
        var original = selected;
        saving = true;
        dialogError = null;
        try
        {
            var response = creating
                ? await MaterialService.CreateAsync(editForm.CreateRequest(Session.Current!.User!.Id), lifetime.Token)
                : await MaterialService.UpdateAsync(editForm.ChangesFrom(original!), lifetime.Token);
            if (disposed) return;
            var saved = RequireData(response);
            if (saved.Id == Guid.Empty || !creating && saved.Id != original!.Id)
                throw new InvalidOperationException("No se pudo confirmar el guardado. Revisa el listado antes de reintentar.");
        }
        catch (OperationCanceledException)
        { if (!disposed) dialogError = "El guardado tardó demasiado. Revisa el listado antes de reintentar: el material podría haberse guardado."; return; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or ArgumentException)
        { if (!disposed) dialogError = ErrorMessage(ex, mutation: true); return; }
        finally { saving = false; }

        CloseDialog();
        status = creating ? "Material agregado." : "Cambios del material guardados.";
        await LoadAsync(creating ? 1 : page);
    }

    private async Task ConfirmStatusAsync()
    {
        if (disposed || ActionBusy || dialogMode != "status" || selected is null || !CheckSession(manage: true)) return;
        var material = selected;
        var newActive = !material.Active;
        saving = true;
        dialogError = null;
        try
        {
            var saved = RequireData(await MaterialService.UpdateAsync(new() { Id = material.Id, Active = newActive }, lifetime.Token));
            if (disposed) return;
            if (saved.Id != material.Id || saved.Active != newActive)
                throw new InvalidOperationException("No se pudo confirmar el estado del material. Revisa el listado.");
        }
        catch (OperationCanceledException)
        { if (!disposed) dialogError = "El cambio de estado tardó demasiado. Revisa el listado antes de reintentar."; return; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed) dialogError = ErrorMessage(ex, mutation: true); return; }
        finally { saving = false; }

        CloseDialog();
        status = newActive ? "Material reactivado." : "Material desactivado.";
        await LoadAsync(materials.Count == 1 && page > 1 && appliedFilter.Active is not null ? page - 1 : page);
    }

    private static T RequireData<T>(ApiResponse<T>? response) where T : class
    {
        if (response?.Data is not { } data || response.Error is { ErrorCode: not 0 })
            throw new InvalidOperationException("La API no devolvió una respuesta válida de material de apoyo.");
        return data;
    }

    private void RememberOptions(SupportMaterialResponse material)
    {
        if (material.ErrorClassificationId != Guid.Empty && !string.IsNullOrWhiteSpace(material.ErrorClassificationName))
            knownCategoryNames[material.ErrorClassificationId] = material.ErrorClassificationName;
    }
    private FileTypeResponse? FileType(SupportMaterialResponse material) => knownFileTypes.GetValueOrDefault(material.FileTypeId);
    private string? ResolveLink(SupportMaterialResponse material) => MaterialPresentation.ResolveLink(material.FileLink, ApiBaseUrl);
    private string CategoryName(SupportMaterialResponse material) => string.IsNullOrWhiteSpace(material.ErrorClassificationName)
        ? knownCategoryNames.GetValueOrDefault(material.ErrorClassificationId, "Sin clasificación") : material.ErrorClassificationName;

    private string ErrorMessage(Exception exception, bool mutation = false)
    {
        if (exception is HttpRequestException http)
        {
            if (http.StatusCode == HttpStatusCode.Unauthorized)
            { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); return "Tu sesión venció. Vuelve a iniciar sesión."; }
            return http.StatusCode switch
            {
                HttpStatusCode.Forbidden => mutation ? "Solo docentes y coordinadores pueden modificar el material." : "No tienes permiso para consultar el material.",
                HttpStatusCode.NotFound => "El material ya no está disponible.",
                HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => "Revisa el nombre, enlace, clasificación y tipo de archivo e inténtalo de nuevo.",
                HttpStatusCode.TooManyRequests => "Hay demasiadas solicitudes. Espera un momento antes de reintentar.",
                _ => mutation ? "No se pudo confirmar el guardado. Revisa el listado antes de reintentar." : "No se pudo consultar el material de apoyo. Inténtalo de nuevo."
            };
        }
        if (exception is InvalidOperationException or ArgumentException) return exception.Message;
        return mutation ? "No se pudo interpretar la respuesta del guardado. Revisa el listado antes de reintentar." : "La respuesta de material de apoyo no tiene el formato esperado.";
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        searchCancellation?.Cancel();
        lifetime.Cancel(); lifetime.Dispose();
    }
}
