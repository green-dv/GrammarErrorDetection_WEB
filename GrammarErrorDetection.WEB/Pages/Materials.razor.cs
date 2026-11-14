using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Materials;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.WEB.Views.Materials;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components.Forms;

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
    private IBrowserFile? pendingFile;
    private InputFile? fileInput;
    private bool useLink;
    private string DialogClass => dialogMode switch
    {
        "preview" => "material-viewer-dialog",
        "create" or "edit" when !useLink => "material-upload-dialog has-preview",
        _ => "material-upload-dialog"
    };

    private MaterialView ViewOf(SupportMaterialResponse material)
    {
        var type = FileType(material);
        var link = ResolveLink(material);
        return new MaterialView(material.Name, CategoryName(material), null,
            MaterialPresentation.Format(material, type), MaterialPresentation.Kind(material, type),
            material.FileSize, material.UploadDate,
            string.IsNullOrWhiteSpace(material.UploadedByName) ? null : material.UploadedByName,
            link, type?.Extension);
    }
    private string DetectedType => fileTypes.FirstOrDefault(item => item.Id.ToString("D") == editForm?.FileTypeId)?.Name
        ?? (selected is not null && selected.FileTypeId.ToString("D") == editForm?.FileTypeId ? FileType(selected)?.Name : null)
        ?? "Se detectará al seleccionar el archivo";
    private const long MaxFileBytes = 25 * 1024 * 1024;
    private bool loading, loadingDetail, loadingCategories, loadingFileTypes, saving, hasNext, disposed;
    private int page = 1, pageSize = 10, loadedPageSize = 10, requestVersion;
    private bool CanManage => Session.IsInRole("Teacher") || Session.IsInRole("Coordinator");
    // Solo los estudiantes registran vistas y marcan materiales como útiles (la API lo restringe al rol Student).
    private bool IsStudent => Session.IsInRole("Student");
    private bool onlyHelpful;
    private readonly HashSet<Guid> voting = [];
    private string commentDraft = "";
    private bool commentSent;
    private string? feedbackError;
    private Task? pendingView;
    private bool ActionBusy => saving || loadingDetail;
    private string ApiBaseUrl => Configuration["Api:BaseUrl"]!;
    private string DialogTitle => dialogMode switch
    {
        "create" => "Agregar material", "edit" => "Editar material",
        "status" => selected?.Active == true ? "Desactivar material" : "Reactivar material",
        "preview" when selected is { } current => current.Name,
        _ => "Material de apoyo"
    };
    private string? DialogSubtitle => dialogMode == "preview" && selected is { } current
        ? $"{CategoryName(current)} · {MaterialPresentation.Kind(current, FileType(current))}" : null;
    private string EmptyTitle => appliedFilter.OnlyHelpful == true
        ? "Aún no marcaste materiales como útiles" : "No hay materiales con estos filtros";
    private string EmptyHint => appliedFilter.OnlyHelpful == true
        ? "Usa «Marcar útil» en los materiales que te ayuden para encontrarlos aquí rápidamente."
        : "Prueba otro nombre, clasificación o tipo de archivo.";
    private string ResultRange => materials.Count == 0 ? "0 resultados"
        : $"Mostrando {(page - 1) * loadedPageSize + 1}–{(page - 1) * loadedPageSize + materials.Count}";
    private bool HasChanges => editForm is not null && (pendingFile is not null || dialogMode == "create" || selected is not null && (
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
            Active = CanManage ? bool.TryParse(activeFilter, out var active) ? active : null : true,
            OnlyHelpful = IsStudent && onlyHelpful ? true : null
        };
        return LoadAsync(1);
    }

    private Task ClearFiltersAsync()
    {
        if (loading || ActionBusy) return Task.CompletedTask;
        name = categoryId = fileTypeId = "";
        activeFilter = "true";
        onlyHelpful = false;
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
        pendingFile = null;
        useLink = false;
        editForm = new();
        dialogError = null;
        dialogMode = "create";
    }

    private async Task OpenDetailAsync(SupportMaterialResponse material, bool edit)
    {
        if (ActionBusy || !CheckSession(edit)) return;
        selected = null; editForm = null; dialogError = null;
        pendingFile = null;
        dialogMode = edit ? "edit" : "preview";
        loadingDetail = true;
        try
        {
            var detail = RequireData(await MaterialService.GetAsync(material.Id, lifetime.Token));
            if (disposed) return;
            if (detail.Id != material.Id) throw new InvalidOperationException("No se recibió el material solicitado.");
            selected = detail;
            RememberOptions(detail);
            commentDraft = "";
            commentSent = false;
            feedbackError = null;
            // Abrir la vista previa cuenta como vista del estudiante (habilita su valoración y alimenta las recomendaciones).
            if (!edit && IsStudent && detail.Active && detail.ViewerSeen != true) pendingView = RegisterViewAsync(detail);
            if (edit)
            {
                editForm = MaterialEditForm.From(detail);
                useLink = !IsUploadedFile(detail.FileLink);
            }
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
        pendingFile = null;
    }

    private void SelectFile(InputFileChangeEventArgs args)
    {
        if (saving || useLink || editForm is null) return;
        pendingFile = null;
        dialogError = null;
        if (args.File.Size is <= 0 or > MaxFileBytes)
        { dialogError = "Selecciona un archivo con contenido de hasta 25 MiB."; return; }
        pendingFile = args.File;
        if (string.IsNullOrWhiteSpace(editForm.Name)) editForm.Name = Path.GetFileNameWithoutExtension(args.File.Name);
        DetectFileType();
    }

    private static bool IsUploadedFile(string link) => link.StartsWith("/archivos/", StringComparison.OrdinalIgnoreCase)
        || Uri.TryCreate(link, UriKind.Absolute, out var uri) && uri.AbsolutePath.StartsWith("/archivos/", StringComparison.OrdinalIgnoreCase);

    private void ChangeSource(bool link)
    {
        if (saving || editForm is null || useLink == link) return;
        useLink = link;
        pendingFile = null;
        dialogError = null;
        var restore = selected is not null && useLink == !IsUploadedFile(selected.FileLink);
        editForm.FileLink = restore ? selected!.FileLink : "";
        editForm.FileSize = restore ? selected!.FileSize : null;
        editForm.FileTypeId = restore ? selected!.FileTypeId.ToString("D") : "";
        if (useLink && !restore) UpdateLinkType();
    }

    private void DetectFileType()
    {
        if (editForm is null || pendingFile is null) return;
        editForm.FileTypeId = MaterialFileTypes.ForFile(fileTypes, pendingFile.Name, pendingFile.ContentType)?.Id.ToString("D") ?? "";
    }

    private void UpdateLinkType()
    {
        if (!useLink || editForm is null) return;
        editForm.FileTypeId = MaterialFileTypes.ForLink(fileTypes)?.Id.ToString("D") ?? "";
        // La API interpreta null en PUT como "conservar"; cero elimina el peso del archivo reemplazado.
        editForm.FileSize = 0;
    }

    private async Task SaveAsync()
    {
        if (disposed || ActionBusy || loadingCategories || loadingFileTypes || editForm is null || !CheckSession(manage: true)
            || dialogMode is not ("create" or "edit")) return;
        if (useLink && (dialogMode == "create" || editForm.FileLink.Trim() != selected?.FileLink)) UpdateLinkType();
        if (!useLink) DetectFileType();
        var errors = editForm.Validate(ApiBaseUrl, !useLink && pendingFile is not null);
        if (useLink && (dialogMode == "create" || editForm.FileLink.Trim() != selected?.FileLink)
            && (!Uri.TryCreate(editForm.FileLink.Trim(), UriKind.Absolute, out var linkUri)
            || linkUri.Scheme is not ("https" or "http"))) errors.Add("Ingresa un enlace HTTP o HTTPS completo.");
        if (dialogMode == "create" && pendingFile is null && string.IsNullOrWhiteSpace(editForm.FileLink))
            errors.Add("Selecciona el archivo que deseas subir.");
        if (!categories.Any(item => item.Id.ToString("D") == editForm.ErrorClassificationId)
            && selected?.ErrorClassificationId.ToString("D") != editForm.ErrorClassificationId)
            errors.Add("Selecciona una clasificación disponible.");
        if (!fileTypes.Any(item => item.Id.ToString("D") == editForm.FileTypeId)
            && selected?.FileTypeId.ToString("D") != editForm.FileTypeId)
            errors.Add(useLink ? "No hay un tipo de enlace activo en el catálogo." : "No se encontró un tipo activo para este archivo en el catálogo.");
        if (errors.Count > 0) { dialogError = string.Join(" ", errors); return; }
        if (!HasChanges) return;
        var creating = dialogMode == "create";
        var original = selected;
        saving = true;
        dialogError = null;
        try
        {
            if (!useLink && pendingFile is { } file)
            {
                await using var stream = file.OpenReadStream(MaxFileBytes, lifetime.Token);
                var uploaded = RequireData(await MaterialService.UploadFileAsync(stream, file.Name, lifetime.Token));
                if (disposed) return;
                if (MaterialPresentation.ResolveLink(uploaded.FileLink, ApiBaseUrl) is null || uploaded.FileSize != file.Size)
                    throw new InvalidOperationException("El servidor no confirmó el archivo subido.");
                editForm.FileLink = uploaded.FileLink;
                editForm.FileSize = uploaded.FileSize;
                editForm.UploadDate = DateTime.Now;
                pendingFile = null;
            }
            else if (useLink && (creating || editForm.FileLink.Trim() != original?.FileLink))
                editForm.UploadDate = DateTime.Now;
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
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or ArgumentException or IOException)
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

    private async Task RegisterViewAsync(SupportMaterialResponse material)
    {
        try
        {
            var response = await MaterialService.RegisterViewAsync(material.Id, false, lifetime.Token);
            if (response?.Error is null) SetInteraction(material.Id, seen: true, helpful: null);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException or ArgumentException) { }
    }

    // "Marcar útil" solo marca (nunca desmarca): responder "No" excluye el material de las recomendaciones,
    // por eso cambiar de opinión se hace explícitamente con Sí / No en la vista previa.
    private async Task VoteAsync(SupportMaterialResponse material, bool helpful, string? comment = null)
    {
        if (!IsStudent || disposed || !voting.Add(material.Id)) return;
        feedbackError = null;
        try
        {
            if (pendingView is { } view) await view;
            if (material.ViewerSeen != true)
            {
                var viewResponse = await MaterialService.RegisterViewAsync(material.Id, false, lifetime.Token);
                if (viewResponse?.Error is { } viewError) throw new InvalidOperationException(viewError.ErrorMessage);
            }
            var response = await MaterialService.SetFeedbackAsync(material.Id, helpful, comment, lifetime.Token);
            if (response?.Error is { } apiError) throw new InvalidOperationException(apiError.ErrorMessage);
            SetInteraction(material.Id, seen: true, helpful: helpful);
            if (comment is not null) commentSent = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or ArgumentException)
        {
            feedbackError = "No se pudo guardar tu respuesta. Inténtalo de nuevo.";
            if (dialogMode != "preview") status = feedbackError;
        }
        catch (OperationCanceledException) { }
        finally { voting.Remove(material.Id); }
    }

    // La tarjeta del listado y el detalle abierto son objetos distintos: se actualizan ambos.
    private void SetInteraction(Guid id, bool seen, bool? helpful)
    {
        var targets = materials.Where(item => item.Id == id).ToList();
        if (selected is { } current && current.Id == id && !targets.Contains(current)) targets.Add(current);
        foreach (var item in targets)
        {
            if (seen) item.ViewerSeen = true;
            if (helpful is { } value) item.ViewerHelpful = value;
        }
    }

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
                HttpStatusCode.MethodNotAllowed => "La API en ejecución no tiene habilitada esta operación. Reinicia la API con la versión que incluye la subida de archivos.",
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
