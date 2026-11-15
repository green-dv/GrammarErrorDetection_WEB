using System.Net.Http.Headers;

namespace GrammarErrorDetection.Infraestructure.ApiClients;

public sealed class ApiAuthorizationHandler(Uri apiBaseUri, Func<string?> getAccessToken) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;
        if (uri is not null && uri.Scheme == Uri.UriSchemeHttps && uri.Host.Equals(apiBaseUri.Host, StringComparison.OrdinalIgnoreCase) && uri.Port == apiBaseUri.Port)
        {
            var token = getAccessToken();
            if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return base.SendAsync(request, cancellationToken);
    }
}