namespace GrammarErrorDetection.Infraestructure.Interfaces;

public interface IApiClient
{
    Task<ApiFileResponse> GetFileAsync(string requestUri, CancellationToken cancellationToken = default);
    Task<TResponse?> PostContentAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default);

    Task<TResponse?> PutContentAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default);

    Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest data, CancellationToken cancellationToken = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest data, CancellationToken cancellationToken = default);
}

public sealed record ApiFileResponse(byte[] Content, string FileName, string ContentType);
