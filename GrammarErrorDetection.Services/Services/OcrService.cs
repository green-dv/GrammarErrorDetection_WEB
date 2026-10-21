using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OcrResponse>(cancellationToken: cancellationToken);
    }
}
