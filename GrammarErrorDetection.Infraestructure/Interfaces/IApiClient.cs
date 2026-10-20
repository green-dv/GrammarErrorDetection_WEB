namespace GrammarErrorDetection.Infraestructure.Interfaces;

public interface IApiClient
{
    Task<TResponse?> GetAsync<TResponse>(
        string requestUri, CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string requestUri, TRequest data, CancellationToken cancellationToken = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(
        string requestUri, TRequest data, CancellationToken cancellationToken = default);
}
