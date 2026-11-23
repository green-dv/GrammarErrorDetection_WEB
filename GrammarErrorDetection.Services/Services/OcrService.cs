using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GrammarErrorDetection.Infraestructure.ApiClients;
using GrammarErrorDetection.Models;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class OcrService(HttpClient httpClient) : IOcrService
{
    public async Task<OcrResponse?> RecognizeAsync(
        Stream image,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        using var imageContent = new StreamContent(image);
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(imageContent, "file", Path.GetFileName(fileName));

        using var response = await httpClient.PostAsync("api/ocr", form, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await ReadErrorAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<OcrResponse>(cancellationToken: cancellationToken);
    }

    // El servicio OCR (FastAPI) explica sus errores en {"detail": "..."}; se conserva para mostrarlo.
    private static async Task<ApiException> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string? detail = null;
        try
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
            if (body.RootElement.ValueKind == JsonValueKind.Object && body.RootElement.TryGetProperty("detail", out var text) && text.ValueKind == JsonValueKind.String)
                detail = text.GetString();
        }
        catch (JsonException) { }
        return new ApiException(detail ?? $"El servicio OCR respondió con HTTP {(int)response.StatusCode}.", response.StatusCode, detail, null);
    }
}
