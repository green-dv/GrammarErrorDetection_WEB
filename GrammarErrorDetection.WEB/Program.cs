using GrammarErrorDetection.WEB;
using GrammarErrorDetection.Infraestructure.ApiClients;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Users;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Falta configurar Api:BaseUrl.");
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiBaseUri)
    || apiBaseUri.Scheme != Uri.UriSchemeHttps)
    throw new InvalidOperationException("La API de autenticación debe usar una URL HTTPS absoluta.");

builder.Services.AddScoped<IApiClient>(provider =>
{
    var session = provider.GetRequiredService<AuthSessionState>();
    var handler = new ApiAuthorizationHandler(apiBaseUri, () => session.Current?.AccessToken) { InnerHandler = new HttpClientHandler() };
    return new ApiClient(new HttpClient(handler) { BaseAddress = apiBaseUri });
});
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<LoginFlowState>();
builder.Services.AddScoped<RegistrationFlowState>();
builder.Services.AddScoped<LoginFlowService>();
builder.Services.AddScoped<AuthSessionState>();

await builder.Build().RunAsync();
