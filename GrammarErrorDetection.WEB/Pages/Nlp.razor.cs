using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Texts;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Pages;

public partial class Nlp : IDisposable
{
    private const long MaxPhotoSize = 5 * 1024 * 1024;
    [Inject] private ITextService Texts { get; set; } = default!;
    [Inject] private IOcrService Ocr { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private string text = "", search = "", appliedSearch = "";
    private string? historyError, uploadError, status, photoMessage, photoError;
    private int currentPage = 1, pageSize = 5, requestVersion;
    private bool allTexts, loading, uploading, recognizingPhoto, photoMenuOpen, hasNext, disposed;
    private List<TextResponse> paragraphs = [];
    private TextResponse? selected;
    private object devicePhotoInputKey = new(), cameraPhotoInputKey = new();
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? historyCancellation;
    private bool CanReadAll => Session.IsInRole("Teacher") || Session.IsInRole("Coordinator");
    private bool EditorBusy => uploading || recognizingPhoto;
    private TextResponse? CurrentAnalysis => selected?.OriginalContent == text ? selected : null;
    protected override Task OnInitializedAsync() => LoadHistoryAsync(1);

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
                throw new InvalidOperationException("No se pudo consultar el historial de párrafos.");
            if (scopeUser is { } owner && response.Data.Any(item => item.UserId != owner))
                throw new InvalidOperationException("La respuesta del historial no corresponde al usuario solicitado.");
            // Count indica las filas recibidas, no el total de páginas.
            if (requestedPage > currentPage && response.Data.Count == 0) { hasNext = false; return; }
            paragraphs = response.Data;
            currentPage = requestedPage;
            hasNext = paragraphs.Count == requestedSize;
        }
        catch (OperationCanceledException)
        { if (!disposed && version == requestVersion && !cancellation.IsCancellationRequested) historyError = "La consulta tardó demasiado. Inténtalo de nuevo."; }
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
        status = uploadError = photoMessage = photoError = null;
        photoMenuOpen = false;
    }

    private async Task UploadAsync()
    {
        if (disposed || EditorBusy || loading || !CheckSession()) return;
        var content = text.Trim();
        if (content.Length == 0 || content.Length > 5000) { uploadError = "Escribe un párrafo de entre 1 y 5000 caracteres."; return; }
        var owner = Session.Current!.User!.Id;
        uploading = true; uploadError = status = photoError = null;
        photoMenuOpen = false;
        try
        {
            var response = await Texts.UploadAsync(new(content), lifetime.Token);
            if (disposed) return;
            if (response?.Data is not { } result || response.Error is { ErrorCode: not 0 }
                || result.Id == Guid.Empty || result.UserId != owner)
                throw new InvalidOperationException("No se pudo confirmar el análisis. Consulta el historial antes de volver a subir el párrafo.");
            selected = result; text = result.OriginalContent;
            status = IsProcessed(result) ? "Párrafo guardado y analizado." : $"Párrafo guardado. Estado: {result.Status}.";
            search = appliedSearch = "";
            await LoadHistoryAsync(1);
        }
        catch (OperationCanceledException)
        { if (!disposed) uploadError = "El análisis tardó demasiado. Consulta el historial antes de reintentar: el párrafo podría haberse guardado."; }
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
                throw new InvalidOperationException("No se pudo actualizar el análisis seleccionado.");
            if (!CanReadAll && data[0].UserId != Session.Current!.User!.Id)
                throw new InvalidOperationException("El análisis no corresponde a tu usuario.");
            selected = data[0]; text = selected.OriginalContent;
        }
        catch (OperationCanceledException) { if (!disposed) uploadError = "La consulta del análisis tardó demasiado."; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed) uploadError = ErrorMessage(ex, false); }
        finally { loading = false; }
    }

    private string ErrorMessage(Exception ex, bool upload)
    {
        if (ex is HttpRequestException http)
        {
            if (http.StatusCode == HttpStatusCode.Unauthorized)
            { Session.Clear(); CheckSession(); return "Tu sesión venció. Vuelve a iniciar sesión."; }
            return http.StatusCode switch
            {
                HttpStatusCode.Forbidden => "No tienes permiso para consultar o analizar estos párrafos.",
                HttpStatusCode.BadRequest => "La API rechazó el párrafo o los filtros. Revisa el contenido e inténtalo de nuevo.",
                HttpStatusCode.TooManyRequests => "Hay demasiadas solicitudes. Espera un momento antes de reintentar.",
                _ => upload ? "No se pudo confirmar el análisis. Revisa el historial antes de reintentar para evitar duplicados." : "No se pudo consultar el historial. Inténtalo de nuevo."
            };
        }
        return ex is InvalidOperationException ? ex.Message : "La respuesta del análisis no tiene el formato esperado.";
    }

    private static bool IsProcessed(TextResponse item) => string.Equals(item.Status, "Procesado", StringComparison.OrdinalIgnoreCase);
    private static string CorrectionText(TextSentenceResponse sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence.CorrectedSentence)) return "";
        var correction = WebUtility.HtmlDecode(sentence.CorrectedSentence).Trim();
        var marker = correction.IndexOf("Corrección:", StringComparison.OrdinalIgnoreCase);
        if (marker >= 0) correction = correction[(marker + "Corrección:".Length)..].Trim();
        foreach (var ruleMarker in new[] { "Regla:", "Rule:" })
        {
            var rule = correction.IndexOf(ruleMarker, StringComparison.OrdinalIgnoreCase);
            if (rule >= 0) correction = correction[..rule].Trim();
        }
        return correction.Trim('*', ' ', '\t', '\r', '\n');
    }
    private static string Title(TextResponse item)
    {
        var title = string.Join(" ", item.OriginalContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Take(6));
        return title.Length > 65 ? title[..65] + "…" : title.Length == 0 ? "Sin contenido" : title;
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
            photoError = "La imagen seleccionada está vacía.";
            return;
        }
        if (photo.Size > MaxPhotoSize)
        {
            photoError = "Selecciona una imagen de hasta 5 MB.";
            return;
        }
        if (string.IsNullOrWhiteSpace(photo.ContentType)
            || !photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            photoError = "El archivo seleccionado debe ser una imagen.";
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
                throw new InvalidOperationException("El OCR no encontró texto en la imagen.");

            text = result.Paragraph.Trim();
            selected = null;
            photoMessage = text.Length > 5000
                ? "Texto extraído. Supera los 5000 caracteres; recórtalo antes de subir el párrafo."
                : result.LineCount > 0
                    ? $"Texto extraído de {result.LineCount} líneas. Revísalo antes de subir el párrafo."
                    : "Texto extraído. Revísalo antes de subir el párrafo.";
        }
        catch (OperationCanceledException)
        {
            if (!disposed) photoError = "La extracción de texto tardó demasiado. Inténtalo de nuevo.";
        }
        catch (HttpRequestException ex)
        {
            if (!disposed) photoError = OcrErrorMessage(ex);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or IOException)
        {
            if (!disposed) photoError = ex is InvalidOperationException
                ? ex.Message
                : "No se pudo leer la respuesta del servicio OCR. Inténtalo de nuevo.";
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
        HttpStatusCode.RequestEntityTooLarge => "La imagen es demasiado grande para el servicio OCR.",
        HttpStatusCode.UnsupportedMediaType => "El servicio OCR no admite el formato de esta imagen.",
        HttpStatusCode.BadRequest => "El servicio OCR no pudo procesar esta imagen.",
        _ => "No se pudo conectar con el servicio OCR en https://localhost:8443. Verifica que esté disponible e inténtalo de nuevo."
    };

    public void Dispose()
    {
        disposed = true;
        historyCancellation?.Cancel();
        lifetime.Cancel(); lifetime.Dispose();
    }
}
