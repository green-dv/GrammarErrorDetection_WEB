using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Texts;
using GrammarErrorDetection.Models.Vocabulary;
using GrammarErrorDetection.WEB.Components.Analysis;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Pages;

public partial class Nlp : IDisposable
{
    private const long MaxPhotoSize = 5 * 1024 * 1024;
    [Inject] private ITextService Texts { get; set; } = default!;
    [Inject] private IRegistrationService Registrations { get; set; } = default!;
    [Inject] private IOcrService Ocr { get; set; } = default!;
    [Inject] private IVocabularyService VocabularyLookup { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private string text = "", search = "", appliedSearch = "";
    private string? historyError, uploadError, status, photoMessage, photoError;
    private int currentPage = 1, pageSize = 5, requestVersion;
    private bool allTexts, loading, uploading, recognizingPhoto, photoMenuOpen, hasNext, disposed;
    private List<TextResponse> paragraphs = [];
    private TextResponse? selected;
    private Guid? registrationId;
    private List<StudentCourseResponse> uploadCourses = [];
    private bool loadingCourses;
    private string? coursesError;
    private object devicePhotoInputKey = new(), cameraPhotoInputKey = new();
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? historyCancellation;
    private bool CanReadAll => Session.IsInRole("Teacher") || Session.IsInRole("Coordinator");
    private bool EditorBusy => uploading || recognizingPhoto;
    private TextResponse? CurrentAnalysis => selected?.OriginalContent == text ? selected : null;
    protected override Task OnInitializedAsync() => Task.WhenAll(LoadHistoryAsync(1), LoadUploadCoursesAsync());

    // --- Pestaña "Vocabulary": alternativa entre errores gramaticales y recomendaciones de vocabulario.
    private AnalysisView analysisView = AnalysisView.Grammar;
    private VocabularyAnalysisResponse? vocabulary;
    // Párrafo cuya verificación se pidió (o se cargó): evita pedirla de nuevo al volver a mostrar la pestaña.
    private Guid vocabularyTextId;
    private string? vocabularyError;
    private bool loadingVocabulary;
    private int vocabularyVersion;
    private CancellationTokenSource? vocabularyCancellation;

    private void ChangeView(AnalysisView view) => analysisView = view;
    private Task ReloadVocabularyAsync() => LoadVocabularyAsync();

    private VocabularyAnalysisResponse? VocabularyFor(TextResponse analysis) => vocabulary?.TextId == analysis.Id ? vocabulary : null;
    private string? VocabularyErrorFor(TextResponse analysis) => vocabularyTextId == analysis.Id ? vocabularyError : null;
    // Pendiente: cargando, o a punto de cargar (la carga empieza tras mostrar la pestaña).
    private bool VocabularyPending(TextResponse analysis) => loadingVocabulary || vocabularyTextId != analysis.Id;

    private bool NeedsVocabulary => !disposed && !loadingVocabulary
        && CurrentAnalysis is { Sentences.Count: > 0 } analysis && IsProcessed(analysis) && analysis.Id != vocabularyTextId;

    // Se carga al mostrar el análisis (el párrafo completo también marca el vocabulario), no solo al abrir su pestaña.
    // Cubre todos los caminos que cambian el párrafo mostrado (historial, nueva subida, actualizar) sin enganchar cada uno.
    protected override Task OnAfterRenderAsync(bool firstRender) => NeedsVocabulary ? LoadVocabularyAsync() : Task.CompletedTask;

    private async Task LoadVocabularyAsync()
    {
        if (disposed || CurrentAnalysis is not { } analysis || !CheckSession()) return;
        var id = analysis.Id;
        vocabularyCancellation?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        vocabularyCancellation = cancellation;
        var version = ++vocabularyVersion;
        vocabularyTextId = id; vocabulary = null; vocabularyError = null; loadingVocabulary = true;
        StateHasChanged();
        try
        {
            var response = await VocabularyLookup.GetAsync(id, cancellation.Token);
            if (disposed || version != vocabularyVersion) return;
            if (response?.Data is not { } data || response.Error is { ErrorCode: not 0 } || data.TextId != id)
                throw new InvalidOperationException(I18n.T("No se pudo cargar el vocabulario de este párrafo."));
            vocabulary = data;
        }
        catch (OperationCanceledException)
        { if (!disposed && version == vocabularyVersion && !cancellation.IsCancellationRequested) vocabularyError = I18n.T("La verificación del vocabulario tardó demasiado. Inténtalo de nuevo."); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed && version == vocabularyVersion) vocabularyError = VocabularyErrorMessage(ex); }
        finally
        {
            if (version == vocabularyVersion)
            {
                loadingVocabulary = false;
                vocabularyCancellation = null;
                if (!disposed) StateHasChanged();
            }
        }
    }

    private string VocabularyErrorMessage(Exception ex)
    {
        if (ex is HttpRequestException http)
        {
            if (http.StatusCode == HttpStatusCode.Unauthorized)
            { Session.Clear(); CheckSession(); return I18n.T("Tu sesión venció. Vuelve a iniciar sesión."); }
            return http.StatusCode switch
            {
                HttpStatusCode.Forbidden => I18n.T("No tienes permiso para consultar el vocabulario de este párrafo."),
                HttpStatusCode.NotFound => I18n.T("No se encontró el párrafo seleccionado."),
                HttpStatusCode.TooManyRequests => I18n.T("Hay demasiadas consultas de vocabulario. Espera un momento antes de reintentar."),
                _ => I18n.T("No se pudo verificar el vocabulario. Inténtalo de nuevo.")
            };
        }
        return ex is InvalidOperationException ? ex.Message : I18n.T("La respuesta del vocabulario no tiene el formato esperado.");
    }

    private async Task LoadUploadCoursesAsync()
    {
        if (disposed || loadingCourses || !Session.IsInRole("Student") || Session.Current?.User is not { } viewer) return;
        loadingCourses = true; coursesError = null;
        try
        {
            var courses = new Dictionary<Guid, StudentCourseResponse>();
            for (var number = 1; ; number++)
            {
                var response = await Registrations.GetMyCoursesAsync(number, 100, lifetime.Token);
                if (disposed || Session.Current?.User?.Id != viewer.Id) return;
                if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException(I18n.T("No se pudieron consultar tus cursos para asociar el párrafo."));
                var added = 0;
                foreach (var course in response.Data)
                    if (course.RegistrationId != Guid.Empty && courses.TryAdd(course.RegistrationId, course)) added++;
                if (response.Data.Count < 100) break;
                if (added == 0) throw new InvalidOperationException(I18n.T("El listado de cursos no avanzó. Inténtalo de nuevo."));
            }
            uploadCourses = courses.Values.Where(course => course.ClassId != Guid.Empty && string.Equals(course.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(course => course.Year).ThenByDescending(course => course.Bimester).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or OperationCanceledException)
        { if (!disposed && !lifetime.IsCancellationRequested) coursesError = ErrorMessage(ex, false); }
        finally { loadingCourses = false; }
    }

    private bool CheckSession()
    {
        if (Session.Current?.User is not null) return true;
        Navigation.NavigateTo("/auth/login", replace: true);
        return false;
    }

    private Task SearchAsync()
    {
        if (EditorBusy) return Task.CompletedTask;
        appliedSearch = search.Trim();
        return LoadHistoryAsync(1);
    }
    private Task ChangeScopeAsync() { NewParagraph(); return LoadHistoryAsync(1); }
    private Task ChangePageSizeAsync() => LoadHistoryAsync(1);
    private Task RetryHistoryAsync() => LoadHistoryAsync(currentPage);
    private Task PreviousPageAsync() => loading || EditorBusy || currentPage <= 1 ? Task.CompletedTask : LoadHistoryAsync(currentPage - 1);
    private Task NextPageAsync() => loading || EditorBusy || !hasNext ? Task.CompletedTask : LoadHistoryAsync(currentPage + 1);

    private async Task LoadHistoryAsync(int requestedPage)
    {
        if (disposed || !CheckSession()) return;
        historyCancellation?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        historyCancellation = cancellation;
        var version = ++requestVersion;
        var userId = Session.Current!.User!.Id;
        var scopeUser = CanReadAll && allTexts ? (Guid?)null : userId;
        var requestedSize = pageSize;
        loading = true;
        historyError = null;
        try
        {
            var response = await Texts.SearchAsync(new()
            {
                PageNumber = requestedPage, PageSize = requestedSize,
                UserId = scopeUser, OriginalContent = appliedSearch
            }, cancellation.Token);
            if (disposed || version != requestVersion) return;
            if (response?.Data is null || response.Error is { ErrorCode: not 0 })
                throw new InvalidOperationException(I18n.T("No se pudo consultar el historial de párrafos."));
            if (scopeUser is { } owner && response.Data.Any(item => item.UserId != owner))
                throw new InvalidOperationException(I18n.T("La respuesta del historial no corresponde al usuario solicitado."));
            // Count indica las filas recibidas, no el total de páginas.
            if (requestedPage > currentPage && response.Data.Count == 0) { hasNext = false; return; }
            paragraphs = response.Data;
            currentPage = requestedPage;
            hasNext = paragraphs.Count == requestedSize;
        }
        catch (OperationCanceledException)
        { if (!disposed && version == requestVersion && !cancellation.IsCancellationRequested) historyError = I18n.T("La consulta tardó demasiado. Inténtalo de nuevo."); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed && version == requestVersion) historyError = ErrorMessage(ex, false); }
        finally
        {
            if (version == requestVersion)
            {
                loading = false;
                historyCancellation = null;
                if (historyError is not null) { paragraphs = []; hasNext = false; }
            }
        }
    }

    private void NewParagraph()
    {
        if (EditorBusy) return;
        selected = null; text = "";
        status = uploadError = photoMessage = photoError = null;
        photoMenuOpen = false;
    }
    private void SelectParagraph(TextResponse paragraph)
    {
        if (EditorBusy) return;
        selected = paragraph; text = paragraph.OriginalContent;
        registrationId = paragraph.UserId == Session.Current?.User?.Id ? paragraph.RegistrationId : null;
        status = uploadError = photoMessage = photoError = null;
        photoMenuOpen = false;
    }

    private async Task UploadAsync()
    {
        if (disposed || EditorBusy || loading || !CheckSession()) return;
        var content = text.Trim();
        if (content.Length == 0 || content.Length > 5000) { uploadError = I18n.T("Escribe un párrafo de entre 1 y 5000 caracteres."); return; }
        var owner = Session.Current!.User!.Id;
        // Docentes y coordinadores no están inscritos: sus párrafos van siempre sin curso.
        var registration = Session.IsInRole("Student") ? registrationId : null;
        if (registration.HasValue && loadingCourses) { uploadError = I18n.T("Espera a que termine la consulta de tus cursos."); return; }
        if (registration.HasValue && !uploadCourses.Any(course => course.RegistrationId == registration))
        { uploadError = I18n.T("Selecciona una inscripción válida para asociar el párrafo."); return; }
        uploading = true; uploadError = status = photoError = null;
        photoMenuOpen = false;
        try
        {
            var response = await Texts.UploadAsync(new(content, registration), lifetime.Token);
            if (disposed) return;
            if (response?.Data is not { } result || response.Error is { ErrorCode: not 0 }
                || result.Id == Guid.Empty || result.UserId != owner || result.RegistrationId != registration)
                throw new InvalidOperationException(I18n.T("No se pudo confirmar el análisis. Consulta el historial antes de volver a subir el párrafo."));
            selected = result; text = result.OriginalContent;
            status = IsProcessed(result) ? I18n.T("Párrafo guardado y analizado.") : I18n.T("Párrafo guardado. Estado: {0}.", result.Status);
            search = appliedSearch = "";
            await LoadHistoryAsync(1);
        }
        catch (OperationCanceledException)
        { if (!disposed) uploadError = I18n.T("El análisis tardó demasiado. Consulta el historial antes de reintentar: el párrafo podría haberse guardado."); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed) uploadError = ErrorMessage(ex, true); }
        finally { uploading = false; }
    }

    private async Task RefreshAnalysisAsync()
    {
        if (selected is null || loading || EditorBusy || !CheckSession()) return;
        var id = selected.Id;
        var original = text;
        loading = true;
        uploadError = null;
        try
        {
            var response = await Texts.SearchAsync(new() { Id = id, PageSize = 1, UserId = CanReadAll && allTexts ? null : Session.Current!.User!.Id }, lifetime.Token);
            if (disposed || selected?.Id != id || text != original) return;
            if (response?.Error is { ErrorCode: not 0 } || response?.Data is not { Count: 1 } data || data[0].Id != id)
                throw new InvalidOperationException(I18n.T("No se pudo actualizar el análisis seleccionado."));
            if (!CanReadAll && data[0].UserId != Session.Current!.User!.Id)
                throw new InvalidOperationException(I18n.T("El análisis no corresponde a tu usuario."));
            selected = data[0]; text = selected.OriginalContent;
        }
        catch (OperationCanceledException) { if (!disposed) uploadError = I18n.T("La consulta del análisis tardó demasiado."); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed) uploadError = ErrorMessage(ex, false); }
        finally { loading = false; }
    }

    private string ErrorMessage(Exception ex, bool upload)
    {
        if (ex is HttpRequestException http)
        {
            if (http.StatusCode == HttpStatusCode.Unauthorized)
            { Session.Clear(); CheckSession(); return I18n.T("Tu sesión venció. Vuelve a iniciar sesión."); }
            return http.StatusCode switch
            {
                HttpStatusCode.Forbidden => I18n.T("No tienes permiso para consultar o analizar estos párrafos."),
                HttpStatusCode.BadRequest => I18n.T("La API rechazó el párrafo o los filtros. Revisa el contenido e inténtalo de nuevo."),
                HttpStatusCode.TooManyRequests => I18n.T("Hay demasiadas solicitudes. Espera un momento antes de reintentar."),
                _ => upload ? I18n.T("No se pudo confirmar el análisis. Revisa el historial antes de reintentar para evitar duplicados.") : I18n.T("No se pudo consultar el historial. Inténtalo de nuevo.")
            };
        }
        return ex is InvalidOperationException ? ex.Message : I18n.T("La respuesta del análisis no tiene el formato esperado.");
    }

    private static bool IsProcessed(TextResponse item) => string.Equals(item.Status, "Procesado", StringComparison.OrdinalIgnoreCase);
    private static string Title(TextResponse item)
    {
        var title = string.Join(" ", item.OriginalContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Take(6));
        return title.Length > 65 ? title[..65] + "…" : title.Length == 0 ? I18n.T("Sin contenido") : title;
    }
    private void TogglePhotoMenu()
    {
        if (EditorBusy) return;
        photoMenuOpen = !photoMenuOpen;
        photoError = null;
    }

    private async Task RecognizePhotoAsync(InputFileChangeEventArgs args)
    {
        photoMenuOpen = false;
        photoMessage = photoError = null;
        if (disposed || EditorBusy) return;

        var photo = args.File;
        if (photo.Size == 0)
        {
            photoError = I18n.T("La imagen seleccionada está vacía.");
            return;
        }
        if (photo.Size > MaxPhotoSize)
        {
            photoError = I18n.T("Selecciona una imagen de hasta 5 MB.");
            return;
        }
        if (string.IsNullOrWhiteSpace(photo.ContentType)
            || !photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            photoError = I18n.T("El archivo seleccionado debe ser una imagen.");
            return;
        }

        recognizingPhoto = true;
        status = uploadError = null;
        try
        {
            await using var image = photo.OpenReadStream(MaxPhotoSize, lifetime.Token);
            var result = await Ocr.RecognizeAsync(image, photo.Name, photo.ContentType, lifetime.Token);
            if (disposed) return;
            if (string.IsNullOrWhiteSpace(result?.Paragraph))
                throw new InvalidOperationException(I18n.T("El OCR no encontró texto en la imagen."));

            text = result.Paragraph.Trim();
            selected = null;
            photoMessage = text.Length > 5000
                ? I18n.T("Texto extraído. Supera los 5000 caracteres; recórtalo antes de subir el párrafo.")
                : result.LineCount > 0
                    ? I18n.T("Texto extraído de {0} líneas. Revísalo antes de subir el párrafo.", result.LineCount)
                    : I18n.T("Texto extraído. Revísalo antes de subir el párrafo.");
        }
        catch (OperationCanceledException)
        {
            if (!disposed) photoError = I18n.T("La extracción de texto tardó demasiado. Inténtalo de nuevo.");
        }
        catch (HttpRequestException ex)
        {
            if (!disposed) photoError = OcrErrorMessage(ex);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or IOException)
        {
            if (!disposed) photoError = ex is InvalidOperationException
                ? ex.Message
                : I18n.T("No se pudo leer la respuesta del servicio OCR. Inténtalo de nuevo.");
        }
        finally
        {
            recognizingPhoto = false;
            devicePhotoInputKey = new();
            cameraPhotoInputKey = new();
        }
    }

    private static string OcrErrorMessage(HttpRequestException exception) => exception.StatusCode switch
    {
        HttpStatusCode.RequestEntityTooLarge => I18n.T("La imagen es demasiado grande para el servicio OCR."),
        HttpStatusCode.UnsupportedMediaType => I18n.T("El servicio OCR no admite el formato de esta imagen."),
        HttpStatusCode.BadRequest => I18n.T("El servicio OCR no pudo procesar esta imagen."),
        _ => "No se pudo conectar con el servicio OCR en https://localhost:8443. Verifica que esté disponible e inténtalo de nuevo."
    };

    public void Dispose()
    {
        disposed = true;
        historyCancellation?.Cancel();
        vocabularyCancellation?.Cancel();
        lifetime.Cancel(); lifetime.Dispose();
    }
}
