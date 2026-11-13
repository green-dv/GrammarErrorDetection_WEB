using System.Net.Http.Json;
using System.Text.Json;
using GrammarErrorDetection.Infraestructure.Interfaces;

namespace GrammarErrorDetection.Infraestructure.ApiClients;

/// <summary>
/// Envía solicitudes JSON o contenido HTTP a las APIs mediante un HttpClient inyectado.
/// Las rutas relativas se resuelven contra su BaseAddress.
/// </summary>
public sealed class ApiClient(HttpClient httpClient) : IApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApiFileResponse> GetFileAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var type = response.Content.Headers.ContentType?.MediaType;
        const string excelType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        if (!string.Equals(type, excelType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El servicio no devolvió un archivo Excel válido.");
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length < 4 || bytes[0] != 0x50 || bytes[1] != 0x4b)
            throw new InvalidOperationException("El archivo Excel recibido está vacío o tiene un formato inválido.");
        var disposition = response.Content.Headers.ContentDisposition;
        var name = (disposition?.FileNameStar ?? disposition?.FileName ?? "reporte-gramatical.xlsx").Trim('"');
        name = Path.GetFileName(name.Replace('\\', '/'));
        name = string.Concat(name.Where(character => !char.IsControl(character)));
        if (string.IsNullOrWhiteSpace(name)) name = "reporte-gramatical.xlsx";
        return new(bytes, name, excelType);
    }

    public async Task<TResponse?> PostContentAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync(requestUri, content, cancellationToken);
        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> PutContentAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsync(requestUri, content, cancellationToken);
        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest data, CancellationToken cancellationToken = default)
    {
        using var response = data is null
            ? await httpClient.PostAsync(requestUri, null, cancellationToken)
            : await httpClient.PostAsJsonAsync(requestUri, data, JsonOptions, cancellationToken);
        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest data, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(requestUri, data, JsonOptions, cancellationToken);
        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    private static async Task<TResponse?> ReadResponseAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return content.Length == 0
            ? default
            : JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken token)
    {
        if (response.IsSuccessStatusCode) return;
        string? message = null;
        try
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
            if (body.RootElement.ValueKind == JsonValueKind.Object && body.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object &&
                error.TryGetProperty("errorMessage", out var text) && text.ValueKind == JsonValueKind.String)
                message = text.GetString();
        }
        catch (JsonException) { }
        throw new HttpRequestException(message ?? $"El servicio respondió con HTTP {(int)response.StatusCode}.", null, response.StatusCode);
    }
}
