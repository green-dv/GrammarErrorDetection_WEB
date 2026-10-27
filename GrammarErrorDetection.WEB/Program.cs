using GrammarErrorDetection.WEB;
using GrammarErrorDetection.WEB.Storage;
using GrammarErrorDetection.Infraestructure.ApiClients;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Users;
using GrammarErrorDetection.Services.Services.Courses;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Falta configurar Api:BaseUrl.");
var ocrBaseUrl = builder.Configuration["Ocr:BaseUrl"]
    ?? throw new InvalidOperationException("Falta configurar Ocr:BaseUrl.");
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiBaseUri)
    || apiBaseUri.Scheme != Uri.UriSchemeHttps)
    throw new InvalidOperationException("La API de autenticación debe usar una URL HTTPS absoluta.");
if (!Uri.TryCreate(ocrBaseUrl, UriKind.Absolute, out var ocrBaseUri)
    || ocrBaseUri.Scheme != Uri.UriSchemeHttps)
    throw new InvalidOperationException("El servicio OCR debe usar una URL HTTPS absoluta.");

builder.Services.AddScoped<IApiClient>(provider =>
{
    var session = provider.GetRequiredService<AuthSessionState>();
    var handler = new ApiAuthorizationHandler(apiBaseUri, () => session.Current?.AccessToken) { InnerHandler = new HttpClientHandler() };
    return new ApiClient(new HttpClient(handler) { BaseAddress = apiBaseUri });
});
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<GrammarErrorDetection.Services.Services.Points.PointRegistrationResolver>();
builder.Services.AddScoped<IPointService, GrammarErrorDetection.Services.Services.Points.PointService>();
builder.Services.AddScoped<ITextService, GrammarErrorDetection.Services.Services.Texts.TextService>();
builder.Services.AddScoped<ISupportMaterialService, GrammarErrorDetection.Services.Services.Materials.SupportMaterialService>();
builder.Services.AddScoped<IOcrService>(_ => new GrammarErrorDetection.Services.Services.OcrService(
    new HttpClient { BaseAddress = ocrBaseUri, Timeout = TimeSpan.FromMinutes(3) }));
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEvaluationService, GrammarErrorDetection.Services.Services.Evaluations.EvaluationService>();
builder.Services.AddScoped<IRegistrationService, GrammarErrorDetection.Services.Services.Registrations.RegistrationService>();
builder.Services.AddScoped<LoginFlowState>();
builder.Services.AddScoped<RegistrationFlowState>();
builder.Services.AddScoped<LoginFlowService>();
builder.Services.AddScoped<AuthSessionState>();
builder.Services.AddScoped<IAuthSessionStorage, BrowserAuthSessionStorage>();

var host = builder.Build();
host.Services.GetRequiredService<AuthSessionState>().Restore();
await host.RunAsync();
