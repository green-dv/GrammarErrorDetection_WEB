using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using GrammarErrorDetection.Infraestructure.ApiClients;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Models.Materials;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.Services.Services.Materials;
using GrammarErrorDetection.WEB.Navigation;
using GrammarErrorDetection.WEB.Pages;
using GrammarErrorDetection.WEB.Views.Materials;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
var materialId = Guid.NewGuid(); var categoryId = Guid.NewGuid(); var typeId = Guid.NewGuid(); var ownerId = Guid.NewGuid();
var handler = new RecordingHandler();
var client = new SupportMaterialService(new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") }));
await client.SearchAsync(new()
{
    PageNumber = 3, PageSize = 10,
    Filter = new() { Id = materialId, Name = " guía & concordancia ", ErrorClassificationId = categoryId, FileTypeId = typeId,
        UploadDate = new(2026, 10, 21), CreatedAt = new(2026, 10, 20), FileLink = "/materials/guía #1.pdf", FileSize = 0, UploadedBy = ownerId, Active = false }
});
Check(handler.Method == HttpMethod.Get && handler.Body is null, "La búsqueda debe usar GET sin cuerpo.");
var query = handler.Path!;
foreach (var value in new[] { "PageNumber=3", "PageSize=10", $"Filter.Id={materialId:D}", "Filter.Name=gu%C3%ADa%20%26%20concordancia",
    $"Filter.ErrorClassificationId={categoryId:D}", $"Filter.FileTypeId={typeId:D}", "Filter.UploadDate=2026-10-21", "Filter.CreatedAt=2026-10-20",
    "Filter.FileLink=%2Fmaterials%2Fgu%C3%ADa%20%231.pdf", "Filter.FileSize=0", $"Filter.UploadedBy={ownerId:D}", "Filter.Active=false" })
    Check(query.Contains(value), $"Falta filtro codificado: {value}");
await client.SearchAsync(new() { Filter = new() { Name = " " } });
Check(handler.Path == "/api/SupportMaterial?PageNumber=1&PageSize=10", "Los filtros vacíos no deben enviarse.");
handler.Json = $$"""{"data":{"id":"{{materialId}}","name":"Guía","errorClassificationName":"Concordancia","uploadedByName":"Ana"},"error":null}""";
var detail = await client.GetAsync(materialId);
Check(handler.Path == $"/api/SupportMaterial/{materialId:D}" && detail?.Data?.ErrorClassificationName == "Concordancia", "El detalle debe conservar los nombres de la API.");
await client.CreateAsync(new() { Name = "Guía", ErrorClassificationId = categoryId, FileTypeId = typeId, UploadedBy = ownerId, FileLink = "/materials/guia.pdf", UploadDate = new(2026, 10, 21) });
using (var json = JsonDocument.Parse(handler.Body!))
{
    var root = json.RootElement;
    Check(handler.Method == HttpMethod.Post && handler.Path == "/api/SupportMaterial", "La creación debe usar el endpoint JSON.");
    Check(root.GetProperty("uploadedBy").GetGuid() == ownerId && root.GetProperty("active").GetBoolean(), "Debe enviar el autor y activo=true.");
    Check(!root.TryGetProperty("fileSize", out _), "El tamaño opcional ausente debe omitirse.");
}
await client.UpdateAsync(new() { Id = materialId, Active = false });
using (var json = JsonDocument.Parse(handler.Body!))
    Check(handler.Method == HttpMethod.Put && json.RootElement.EnumerateObject().Count() == 2 && !json.RootElement.GetProperty("active").GetBoolean(), "La baja debe enviar exclusivamente id y active=false.");
await client.UpdateAsync(new() { Id = materialId, Name = "Nombre actualizado" });
using (var json = JsonDocument.Parse(handler.Body!))
    Check(json.RootElement.EnumerateObject().Count() == 2, "El PUT parcial debe omitir los campos nulos.");
handler.Json = """{"data":[],"error":null}""";
await client.GetClassificationsAsync(2);
Check(handler.Path == "/api/ErrorClassification?PageNumber=2&PageSize=100&Filter.Active=true", "Contrato del catálogo de clasificaciones incorrecto.");
await client.GetFileTypesAsync(3);
Check(handler.Path == "/api/FileType?PageNumber=3&PageSize=100&Filter.Active=true", "Contrato del catálogo de tipos incorrecto.");
handler.Json = """{"data":{},"error":null}""";
await client.GetClassificationAsync(categoryId); Check(handler.Path == $"/api/ErrorClassification/{categoryId:D}", "Ruta de clasificación incorrecta.");
await client.GetFileTypeAsync(typeId); Check(handler.Path == $"/api/FileType/{typeId:D}", "Ruta de tipo incorrecta.");
handler.Json = """{"data":[],"error":null}""";
foreach (var invalid in new SupportMaterialSearchRequest[] { new() { PageNumber = 0 }, new() { PageSize = 0 }, new() { Filter = new() { FileTypeId = Guid.Empty } } })
{
    try { await client.SearchAsync(invalid); throw new Exception("Se aceptó una consulta inválida."); } catch (ArgumentException) { }
}

var baseUrl = "https://api.example.test/";
Check(MaterialPresentation.ResolveLink("/materials/guia.pdf", baseUrl) == "https://api.example.test/materials/guia.pdf", "Las rutas de archivos deben resolverse contra la API.");
foreach (var unsafeLink in new[] { "javascript:alert(1)", "data:text/html,test", "//otro.test/file", @"C:\archivo.pdf", @"\\servidor\archivo", "https://usuario:clave@otro.test" })
    Check(MaterialPresentation.ResolveLink(unsafeLink, baseUrl) is null, $"Se aceptó un enlace no válido: {unsafeLink}");
Check(MaterialPresentation.Kind(new() { FileLink = "/files/1234" }, new() { MimeType = "image/png", Extension = ".png" }) == "Imagen", "La vista previa debe reconocer el tipo incluso sin extensión en la URL.");
var original = new SupportMaterialResponse { Id = materialId, Name = "Guía", FileLink = "/guia.pdf", ErrorClassificationId = categoryId, FileTypeId = typeId, FileSize = 200, UploadedBy = ownerId, UploadDate = new(2026, 10, 21) };
var edit = MaterialEditForm.From(original);
Check(!edit.ChangesFrom(original).HasChanges, "Abrir la edición no debe generar cambios.");
edit.Name = " Guía revisada "; edit.FileSize = null;
var patch = edit.ChangesFrom(original);
Check(patch.Name == "Guía revisada" && patch.UploadedBy is null && patch.UploadDate is null && patch.FileSize is null && patch.Active is null, "Editar el nombre debe preservar autor, fecha, tamaño y estado.");

foreach (var role in new[] { "Student", "Teacher", "Coordinator", "Other" })
{
    Check(PortalNavigation.CanAccess("materials", [role]), "Todos los roles autenticados deben poder entrar.");
    var session = new AuthSessionState();
    session.SignIn(new() { AccessToken = $"test.{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"role\":\"{role}\"}}"))}.test", TokenType = "Bearer", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1), User = new() { Id = ownerId, Email = "check@example.test" } }, "check@example.test");
    var fake = new MaterialFake(categoryId, typeId, ownerId);
    var activator = new CapturingActivator();
    var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = baseUrl }).Build();
    using var services = new ServiceCollection().AddLogging().AddSingleton<ISupportMaterialService>(fake).AddSingleton(session)
        .AddSingleton<IConfiguration>(config).AddSingleton<NavigationManager>(new TestNavigation()).AddSingleton<IJSRuntime>(new NoJs())
        .AddSingleton<IComponentActivator>(activator).BuildServiceProvider();
    await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        var root = await renderer.RenderComponentAsync<Materials>();
        var page = activator.Page!;
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        object? Get(string field) => typeof(Materials).GetField(field, flags)!.GetValue(page);
        void Set(string field, object? value) => typeof(Materials).GetField(field, flags)!.SetValue(page, value);
        object? Call(string method, params object[] args) => typeof(Materials).GetMethod(method, flags)!.Invoke(page, args);
        async Task Invoke(string method, params object[] args) => await (Task)Call(method, args)!;
        var manager = role is "Teacher" or "Coordinator";
        var html = root.ToHtmlString();
        Check(fake.Searches.Count == 1 && fake.Searches[0].Filter?.Active == true, "El listado inicial debe consultar solo activos.");
        Check(fake.CategoryPages.SequenceEqual([1, 2]) && ((List<ErrorClassificationResponse>)Get("categories")!).Count == 101, "Debe cargar todas las páginas de clasificaciones.");
        Check(html.Contains("materials-grid") && html.Contains("material-card") && html.Contains("Concordancia"), "Debe renderizar tarjetas con los datos reales.");
        Check(html.Contains("Agregar material") == manager && html.Contains("title=\"Editar\"") == manager, $"Permisos de UI incorrectos para {role}.");
        if (role is "Teacher" or "Student") WritePreview(role.ToLowerInvariant(), html);
        await Invoke("NextPageAsync");
        Check((int)Get("page")! == 1 && !(bool)Get("hasNext")!, "Una página siguiente vacía debe conservar el listado anterior.");
        Set("name", "guía"); Set("categoryId", categoryId.ToString("D")); Set("fileTypeId", typeId.ToString("D")); Set("activeFilter", "false");
        await Invoke("SearchAsync");
        var filter = fake.Searches[^1].Filter!;
        Check(filter.Name == "guía" && filter.ErrorClassificationId == categoryId && filter.FileTypeId == typeId && filter.Active == !manager, "Los filtros deben llegar al servidor y el alumno debe conservar Active=true.");
        await Invoke("ClearFiltersAsync");
        var material = fake.Rows[0];
        await Invoke("OpenDetailAsync", material, false);
        Check(fake.DetailCalls == 1 && ((SupportMaterialResponse)Get("selected")!).Id == material.Id, "Ver material debe consultar GET por ID.");
        Call("CloseDialog");
        Call("OpenCreate");
        Check((string?)Get("dialogMode") == (manager ? "create" : null), "El acceso directo al modal debe respetar roles.");
        if (role == "Teacher")
        {
            var refresh = typeof(ComponentBase).GetMethod("StateHasChanged", flags)!;
            refresh.Invoke(page, null);
            WritePreview("create", root.ToHtmlString());
        }
        // Intenta invocar la escritura aunque el botón no exista en la UI.
        Set("dialogMode", "create");
        Set("editForm", MaterialEditForm.From(material));
        await Invoke("SaveAsync");
        Check(fake.Creates.Count == (manager ? 1 : 0), "La autorización debe comprobarse también antes del POST.");
        if (manager)
        {
            Check(fake.Creates[0].UploadedBy == ownerId && fake.Creates[0].Active, "El autor debe venir de la sesión.");
            await Invoke("OpenDetailAsync", material, true);
            ((MaterialEditForm)Get("editForm")!).Name = "Nombre editado";
            await Invoke("SaveAsync");
            Check(fake.Updates[^1].Name == "Nombre editado" && fake.Updates[^1].UploadedBy is null, "La edición debe enviar un PUT parcial.");
        }
        Set("dialogMode", "status"); Set("selected", material);
        var updates = fake.Updates.Count;
        await Invoke("ConfirmStatusAsync");
        Check(fake.Updates.Count == updates + (manager ? 1 : 0), "La autorización debe comprobarse antes de desactivar.");
        if (manager) Check(fake.Updates[^1].Id == material.Id && fake.Updates[^1].Active == false && fake.Updates[^1].Name is null, "La desactivación debe ser parcial.");
    });
}
Console.WriteLine("OK: filtros GET, catálogos paginados, JSON parcial, enlaces, render de tarjetas y permisos de lectura/escritura para cuatro roles.");

void WritePreview(string name, string content)
{
    var directory = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "materials-preview");
    Directory.CreateDirectory(directory);
    var styles = "<link rel=\"stylesheet\" href=\"/GrammarErrorDetection.WEB/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css\">" + string.Join("", new[] { "app", "auth/theme", "portal", "tables", "users", "modals", "materials" }
        .Select(style => $"<link rel=\"stylesheet\" href=\"/GrammarErrorDetection.WEB/wwwroot/css/{style}.css\">"));
    File.WriteAllText(Path.Combine(directory, name + ".html"), $"<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">{styles}</head><body class=\"ceti-shell\" style=\"display:block;margin:0;padding:28px;background:#f6f8fb\">{content}<script>document.querySelector('dialog')?.showModal()</script></body></html>");
}

sealed class RecordingHandler : HttpMessageHandler
{
    public HttpMethod? Method; public string? Path, Body; public string Json = "{\"data\":[],\"error\":null}";
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Method = request.Method; Path = request.RequestUri!.PathAndQuery;
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new(HttpStatusCode.OK) { Content = new StringContent(Json, Encoding.UTF8, "application/json") };
    }
}
sealed class CapturingActivator : IComponentActivator
{
    public Materials? Page;
    public IComponent CreateInstance(Type type) { var instance = (IComponent)Activator.CreateInstance(type)!; if (instance is Materials page) Page = page; return instance; }
}
sealed class TestNavigation : NavigationManager
{
    public TestNavigation() => Initialize("https://web.example.test/", "https://web.example.test/materials");
    protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).AbsoluteUri;
}
sealed class NoJs : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ValueTask.FromResult(default(TValue)!);
}
sealed class MaterialFake(Guid categoryId, Guid typeId, Guid ownerId) : ISupportMaterialService
{
    public List<SupportMaterialSearchRequest> Searches = [];
    public List<int> CategoryPages = [];
    public List<SupportMaterialCreateRequest> Creates = [];
    public List<SupportMaterialUpdateRequest> Updates = [];
    public int DetailCalls;
    public List<SupportMaterialResponse> Rows = Enumerable.Range(1, 10).Select(i => new SupportMaterialResponse { Id = Guid.NewGuid(), Name = $"Guía {i}", FileLink = $"/materials/guia-{i}.pdf", ErrorClassificationId = categoryId, ErrorClassificationName = "Concordancia", FileTypeId = typeId, UploadedBy = ownerId, UploadedByName = "Ana Pérez", UploadDate = new(2026, 10, 21), Active = true }).ToList();
    public Task<ApiResponse<List<SupportMaterialResponse>>?> SearchAsync(SupportMaterialSearchRequest request, CancellationToken token = default)
    {
        Searches.Add(request);
        var rows = request.PageNumber > 1 ? [] : Rows.Where(item => request.Filter?.Active is not { } active || item.Active == active).ToList();
        return Task.FromResult<ApiResponse<List<SupportMaterialResponse>>?>(new() { Data = rows, Count = rows.Count });
    }
    public Task<ApiResponse<SupportMaterialResponse>?> GetAsync(Guid id, CancellationToken token = default) { DetailCalls++; return Task.FromResult<ApiResponse<SupportMaterialResponse>?>(new() { Data = Rows.Single(item => item.Id == id) }); }
    public Task<ApiResponse<SupportMaterialResponse>?> CreateAsync(SupportMaterialCreateRequest request, CancellationToken token = default)
    {
        Creates.Add(request); return Task.FromResult<ApiResponse<SupportMaterialResponse>?>(new() { Data = new() { Id = Guid.NewGuid(), Name = request.Name, Active = request.Active } });
    }
    public Task<ApiResponse<SupportMaterialResponse>?> UpdateAsync(SupportMaterialUpdateRequest request, CancellationToken token = default)
    {
        Updates.Add(request); var row = Rows.Single(item => item.Id == request.Id);
        if (request.Active is { } active) row.Active = active;
        if (request.Name is { } name) row.Name = name;
        return Task.FromResult<ApiResponse<SupportMaterialResponse>?>(new() { Data = row });
    }
    public Task<ApiResponse<List<ErrorClassificationResponse>>?> GetClassificationsAsync(int number, CancellationToken token = default)
    {
        CategoryPages.Add(number);
        var rows = number == 1 ? Enumerable.Range(1, 100).Select(i => new ErrorClassificationResponse { Id = Guid.NewGuid(), Name = $"Clasificación {i}", Active = true }).ToList() : new List<ErrorClassificationResponse> { new() { Id = categoryId, Name = "Concordancia", Active = true } };
        return Task.FromResult<ApiResponse<List<ErrorClassificationResponse>>?>(new() { Data = rows, Count = rows.Count });
    }
    public Task<ApiResponse<List<FileTypeResponse>>?> GetFileTypesAsync(int number, CancellationToken token = default) => Task.FromResult<ApiResponse<List<FileTypeResponse>>?>(new() { Data = [new() { Id = typeId, Name = "PDF", Extension = ".pdf", MimeType = "application/pdf", Active = true }] });
    public Task<ApiResponse<ErrorClassificationResponse>?> GetClassificationAsync(Guid id, CancellationToken token = default) => throw new NotSupportedException();
    public Task<ApiResponse<FileTypeResponse>?> GetFileTypeAsync(Guid id, CancellationToken token = default) => throw new NotSupportedException();
}
