using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using GrammarErrorDetection.Infraestructure.ApiClients;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Users;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.WEB.Navigation;
using GrammarErrorDetection.WEB.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

void Check(bool value, string message) { if (!value) throw new Exception(message); }
var handler = new RecordingHandler();
var auth = new ApiAuthorizationHandler(new Uri("https://api.example.test/"), () => "session-token") { InnerHandler = handler };
var api = new ApiClient(new HttpClient(auth) { BaseAddress = new Uri("https://api.example.test/") });
var session = new AuthSessionState();
var queryingUserId = Guid.NewGuid();
session.SignIn(new() { AccessToken = "session-token", TokenType = "Bearer", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
    User = new() { Id = queryingUserId, Email = "leaderboard@example.test" } }, "leaderboard@example.test");
var service = new LeaderboardService(api, session);
var users = new UserService(api);
var result = await service.GetAsync(new());
bool IsUnfilteredRequest()
{
    using var json = JsonDocument.Parse(handler.Body!);
    return json.RootElement.EnumerateObject().Count() == 1 && json.RootElement.GetProperty("userId").GetGuid() == queryingUserId;
}
Check(IsUnfilteredRequest() && handler.Path == "/api/User/leaderboard" && handler.Method == HttpMethod.Post, "La consulta inicial debe enviar POST con el ID de quien consulta y sin filtros.");
Check(handler.Bearer == "Bearer session-token" && handler.ContentType == "application/json", "Debe usar JSON y el token de la sesión.");
Check(result?.Data is { Count: 2 } && result.Data[1].Position is null && result.Data[1].IsCurrentUser && !result.Data[1].IsTop10, "Debe aceptar al usuario actual sin posición y con cero puntos.");
await service.GetAsync(new() { Bimester = 6, Year = 2026, ModuleNameId = handler.ModuleId });
using (var json = JsonDocument.Parse(handler.Body!))
{
    Check(json.RootElement.EnumerateObject().Count() == 4 && json.RootElement.GetProperty("userId").GetGuid() == queryingUserId, "Debe enviar los tres filtros y el UserId de la sesión.");
    Check(json.RootElement.GetProperty("bimester").GetInt32() == 6 && json.RootElement.GetProperty("year").GetInt32() == 2026
        && json.RootElement.GetProperty("moduleNameId").GetGuid() == handler.ModuleId, "Filtros incorrectos.");
}
foreach (var request in new LeaderboardRequest[] { new() { Year = 1 }, new() { Bimester = 1 }, new() { ModuleNameId = handler.ModuleId } })
{
    await service.GetAsync(request);
    using var json = JsonDocument.Parse(handler.Body!);
    Check(json.RootElement.EnumerateObject().Count() == 2, "Los filtros deben poder enviarse independientemente, junto con UserId.");
}
await service.GetAsync(new() { UserId = Guid.NewGuid() });
Check(IsUnfilteredRequest(), "El ID enviado debe proceder de la sesión, aunque el objeto tenga otro ID.");
try { await new LeaderboardService(api, new AuthSessionState()).GetAsync(new()); throw new Exception("Se aceptó una sesión ausente."); }
catch (InvalidOperationException) { }
foreach (var request in new LeaderboardRequest[] { new() { Year = 0 }, new() { Year = -1 }, new() { Bimester = 0 }, new() { Bimester = 7 }, new() { ModuleNameId = Guid.Empty } })
{
    try { await service.GetAsync(request); throw new Exception("Se aceptó un filtro inválido."); } catch (ArgumentException) { }
}
Check(PortalNavigation.CanAccess("leaderboard", ["Student"]), "Los estudiantes deben tener acceso al ranking.");

var activator = new CapturingActivator();
using var services = new ServiceCollection().AddLogging().AddSingleton<ILeaderboardService>(service).AddSingleton<IUserService>(users)
    .AddSingleton<IComponentActivator>(activator).BuildServiceProvider();
await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
await renderer.Dispatcher.InvokeAsync(async () =>
{
    var root = await renderer.RenderComponentAsync<Leaderboard>();
    var html = WebUtility.HtmlDecode(root.ToHtmlString());
    Check(IsUnfilteredRequest() && handler.ModuleCalls == 2, "Debe iniciar con UserId, sin filtros y cargar todas las páginas del catálogo.");
    Check(html.Contains("Todos los tiempos") && html.Contains("Módulo 100") && html.Contains("Fuera del top 10"), "Debe mostrar catálogo, período por defecto y el resultado de quien consulta fuera del top.");
    Check(html.Contains("leaderboard-rank-number\">--</span>") && !html.Contains("leaderboard-current"), "La fila adicional debe mostrar -- sin resaltado de posición.");
    Check(html.IndexOf("Ana Pérez", html.IndexOf("<tbody>")) < html.IndexOf("Luis Rojas"), "El resultado de quien consulta debe aparecer después del top.");
    Check(html.Contains("place-1") && !html.Contains("place-2"), "El usuario sin posición no debe formar parte del podio.");
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
    void Set(string name, string value) => typeof(Leaderboard).GetField(name, flags)!.SetValue(activator.Page, value);
    async Task Call(string name) => await (Task)typeof(Leaderboard).GetMethod(name, flags)!.Invoke(activator.Page, null)!;
    Set("year", "2026"); Set("bimester", "6"); Set("moduleId", handler.ModuleId.ToString());
    await Call("SearchAsync");
    using (var json = JsonDocument.Parse(handler.Body!)) Check(json.RootElement.GetProperty("bimester").GetInt32() == 6, "El formulario debe enviar los filtros seleccionados.");
    var before = handler.RankingCalls;
    Set("year", "0"); await Call("SearchAsync");
    Check(before == handler.RankingCalls, "La validación debe impedir solicitudes inválidas.");
    await Call("ClearAsync"); Check(IsUnfilteredRequest(), "Limpiar debe volver a todos los tiempos conservando el ID de quien consulta.");
    handler.Mode = "empty"; await Call("LoadAsync");
    // Trigger a render after invoking the event methods directly.
    typeof(ComponentBase).GetMethod("StateHasChanged", flags)!.Invoke(activator.Page, null);
    Check(root.ToHtmlString().Contains("No hay resultados"), "Debe mostrar el estado vacío.");
    handler.Mode = "error"; await Call("LoadAsync");
    typeof(ComponentBase).GetMethod("StateHasChanged", flags)!.Invoke(activator.Page, null);
    Check(root.ToHtmlString().Contains("Reintentar"), "Debe permitir reintentar una respuesta de error.");
});
Console.WriteLine("Leaderboard checks passed: authentication, request contract, validation, catalog pagination, rendering, filters, empty and error states.");

sealed class CapturingActivator : IComponentActivator
{
    public Leaderboard? Page { get; private set; }
    public IComponent CreateInstance(Type type)
    {
        var instance = (IComponent)Activator.CreateInstance(type)!;
        if (instance is Leaderboard page) Page = page;
        return instance;
    }
}

sealed class RecordingHandler : HttpMessageHandler
{
    public Guid ModuleId { get; } = Guid.NewGuid();
    public string? Body, Path, Bearer, ContentType;
    public HttpMethod? Method;
    public int ModuleCalls, RankingCalls;
    public string Mode = "success";
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string json;
        if (request.RequestUri!.AbsolutePath == "/api/ModuleName")
        {
            ModuleCalls++;
            var second = request.RequestUri.Query.Contains("PageNumber=2");
            var rows = Enumerable.Range(second ? 100 : 0, second ? 1 : 100).Select(index => new UserLookupOption
                { Id = index == 0 ? ModuleId : Guid.NewGuid(), Name = $"Módulo {index}", Active = true });
            json = JsonSerializer.Serialize(new { data = rows, count = second ? 1 : 100 });
        }
        else
        {
            RankingCalls++;
            Path = request.RequestUri.AbsolutePath; Method = request.Method;
            Bearer = request.Headers.Authorization?.ToString(); ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            json = Mode switch
            {
                "empty" => """{"data":[],"count":0,"error":null}""",
                "error" => """{"data":null,"error":{"errorMessage":"Error de consulta"}}""",
                _ => $$"""{"data":[{"position":1,"userId":"{{Guid.NewGuid()}}","userName":"Ana Pérez","moduleNameId":"{{ModuleId}}","moduleName":"Módulo I","totalPoints":120,"isCurrentUser":false,"isTop10":true},{"position":null,"userId":"{{Guid.NewGuid()}}","firstName":"Luis","lastName":"Rojas","moduleNameId":null,"moduleName":null,"totalPoints":0,"isCurrentUser":true,"isTop10":false}],"count":2,"error":null}"""
            };
        }
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
