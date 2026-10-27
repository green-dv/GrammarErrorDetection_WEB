using System.Net;
using System.Text;
using System.Text.Json;
using GrammarErrorDetection.Models.Points;
using GrammarErrorDetection.Infraestructure.ApiClients;
using GrammarErrorDetection.Services.Services.Points;

static void Check(bool value, string message) { if (!value) throw new Exception(message); }
var handler = new RecordingHandler();
var service = new PointService(new ApiClient(new HttpClient(handler) { BaseAddress = new("https://api.example.test/") }));
var user = Guid.NewGuid(); var course = Guid.NewGuid(); var registration = Guid.NewGuid();
handler.Json = """{"data":{"totalPoints":-10},"error":null}""";
Check((await service.GetTotalAsync(user)).TotalPoints == -10, "Negative totals must deserialize.");
Check(handler.Path == $"/api/PointTransaction/total?UserId={user:D}" && handler.Body is null, "Total must use GET scoped to user.");
await service.GetTotalAsync(user, course);
Check(handler.Path!.EndsWith($"&ClassId={course:D}"), "Optional class filter missing.");
try { await service.GetTotalAsync(Guid.Empty); throw new Exception("Unscoped total was allowed."); } catch (ArgumentException) { }
handler.Json = """{"data":null,"error":null}""";
foreach (var amount in new[] { -15, 0, 99 })
{
    await service.CreateAsync(new() { RegistrationId = registration, Amount = amount, Reason = "Participación" });
    using var body = JsonDocument.Parse(handler.Body!);
    Check(handler.Method == HttpMethod.Post && handler.Path == "/api/PointTransaction", "Wrong creation endpoint.");
    Check(body.RootElement.GetProperty("registrationId").GetGuid() == registration && body.RootElement.GetProperty("amount").GetInt32() == amount, "Wrong assignment data.");
    Check(!body.RootElement.TryGetProperty("grantedAt", out _) && body.RootElement.GetProperty("active").GetBoolean(), "Use DB timestamp and active=true.");
}
await service.UpdateAsync(new() { Id = registration, Active = false });
using (var body = JsonDocument.Parse(handler.Body!))
{
    Check(handler.Method == HttpMethod.Put && body.RootElement.EnumerateObject().Count() == 2, "Partial update must omit unspecified fields.");
    Check(!body.RootElement.GetProperty("active").GetBoolean(), "Deactivation must preserve false.");
}
foreach (var reason in new[] { " ", new string('x', 256) })
{
    try { await service.CreateAsync(new() { RegistrationId = registration, Reason = reason }); throw new Exception("Invalid reason accepted."); } catch (ArgumentException) { }
}
var a = Guid.NewGuid(); var b = Guid.NewGuid();
handler.Responses.Enqueue($$"""{"data":[{"id":"{{a}}","level":1,"name":"Principiante","minPoints":0,"active":true,"totalCount":2}],"count":1,"error":null}""");
handler.Responses.Enqueue($$"""{"data":[{"id":"{{b}}","level":2,"name":"Avanzado","minPoints":100,"active":true,"totalCount":2}],"count":1,"error":null}""");
var levels = await service.GetLevelsAsync();
Check(levels.Count == 2 && handler.Path!.Contains("PageNumber=2"), "Levels must follow totalCount, not page count.");
Check(LevelProgress.From(50, levels) is { Current.Level: 1, Next.Level: 2, Remaining: 50, Percent: 50 }, "Wrong progress inside a level.");
Check(LevelProgress.From(100, levels) is { Current.Level: 2, Next: null, Percent: 100 }, "Threshold must advance the level; final level has no next.");
Check(LevelProgress.From(-10, levels) is { Current: null, Next.Level: 1, Remaining: 10, Percent: 0 }, "Negative totals must not invent a level.");
Check(LevelProgress.From(0, []) is { Current: null, Next: null }, "Empty catalog must not invent a level.");
handler.Json = """{"data":null,"error":{"errorCode":5}}""";
try { await service.GetTotalAsync(user); throw new Exception("API errors must not become zero points."); } catch (InvalidOperationException) { }
var movement = Guid.NewGuid();
handler.Json = $$"""{"data":[{"id":"{{movement}}","registrationId":"{{registration}}","amount":-5,"reason":"Ajuste","grantedAt":"2026-10-21T10:00:00","active":false}],"count":1,"error":null}""";
var history = await service.GetHistoryAsync(user, 2, 5);
Check(handler.Method == HttpMethod.Get && handler.Body is null && handler.Path == $"/api/PointTransaction?Filter.UserId={user:D}&PageNumber=2&PageSize=5", "History must use GET with user filter and requested pagination.");
Check(history.Count == 1 && history[0].Id == movement && history[0].RegistrationId == registration && history[0].Amount == -5 && !history[0].Active && history[0].GrantedAt == new DateTime(2026, 10, 21, 10, 0, 0), "History must preserve date, signed amounts and inactive entries.");
handler.Json = """{"data":[],"count":0,"error":null}""";
Check((await service.GetHistoryAsync(user, 3, 5)).Count == 0, "Empty history pages must remain empty.");
try { await service.GetHistoryAsync(Guid.Empty, 1, 10); throw new Exception("Unscoped history allowed."); } catch (ArgumentException) { }
try { await service.GetHistoryAsync(user, 0, 10); throw new Exception("Invalid history page allowed."); } catch (ArgumentOutOfRangeException) { }
try { await service.GetHistoryAsync(user, 1, 0); throw new Exception("Invalid history page size allowed."); } catch (ArgumentOutOfRangeException) { }
handler.Json = """{"data":[],"count":0,"error":{"errorCode":5}}""";
try { await service.GetHistoryAsync(user, 1, 10); throw new Exception("History API errors must not become an empty page."); } catch (InvalidOperationException) { }
var resolver = new PointRegistrationResolver(
    new GrammarErrorDetection.Services.Services.Courses.CourseService(new ApiClient(new HttpClient(handler) { BaseAddress = new("https://api.example.test/") })),
    new GrammarErrorDetection.Services.Services.Registrations.RegistrationService(new ApiClient(new HttpClient(handler) { BaseAddress = new("https://api.example.test/") })));
handler.Json = $$"""{"data":[{"id":"{{registration}}","userId":"{{user}}","classId":"{{course}}","active":true}],"error":null}""";
var target = await resolver.ResolveAsync(user, course);
Check(target.RegistrationId == registration && target.ClassId == course && handler.Path == $"/api/Class/{course:D}/registrations?PageNumber=1&PageSize=100", "A current course without embedded registrationId must resolve the actual course registration.");
// Sin CurrentCourse: el resumen tampoco devuelve Active; validar en el curso.
handler.Responses.Enqueue($$"""{"data":[{"registrationId":"{{registration}}","userId":"{{user}}","classId":"{{course}}","moduleName":"Módulo 1"}],"error":null}""");
handler.Responses.Enqueue(handler.Json);
target = await resolver.ResolveAsync(user, null);
Check(target.RegistrationId == registration && target.CourseName == "Módulo 1", "Missing current course and Active in summary must not mean no valid registration.");
// No seleccionar inscripciones de otros usuarios ni registros inactivos.
handler.Json = $$"""{"data":[{"id":"{{Guid.NewGuid()}}","userId":"{{Guid.NewGuid()}}","classId":"{{course}}","active":true},{"id":"{{registration}}","userId":"{{user}}","classId":"{{course}}","active":false}],"error":null}""";
try { await resolver.ResolveAsync(user, course); throw new Exception("Inactive or another user's registration selected."); } catch (InvalidOperationException) { }
handler.Json = $$"""{"data":[{"id":"{{registration}}","userId":"{{user}}","classId":"{{course}}","active":true},{"id":"{{Guid.NewGuid()}}","userId":"{{user}}","classId":"{{course}}","active":true}],"error":null}""";
try { await resolver.ResolveAsync(user, course); throw new Exception("Ambiguous duplicate registrations accepted."); } catch (InvalidOperationException) { }
// Si el estudiante figura en una página posterior, continuar la búsqueda.
var fullPage = Enumerable.Range(0, 100).Select(_ => new { id = Guid.NewGuid(), userId = Guid.NewGuid(), classId = course, active = true }).ToArray();
handler.Responses.Enqueue(JsonSerializer.Serialize(new { data = fullPage, error = (string?)null }));
handler.Responses.Enqueue($$"""{"data":[{"id":"{{registration}}","userId":"{{user}}","classId":"{{course}}","active":true}],"error":null}""");
Check((await resolver.ResolveAsync(user, course)).RegistrationId == registration && handler.Path!.Contains("PageNumber=2"), "Course registration lookup must paginate.");
Console.WriteLine("Point checks passed: payloads, filters, partial updates, validation, catalog pagination, level boundaries, history pagination, registration resolution and API errors.");

sealed class RecordingHandler : HttpMessageHandler
{
    public string Json = "{}";
    public Queue<string> Responses = new();
    public string? Path, Body;
    public HttpMethod? Method;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Path = request.RequestUri!.PathAndQuery; Method = request.Method;
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(token);
        return new(HttpStatusCode.OK) { Content = new StringContent(Responses.Count > 0 ? Responses.Dequeue() : Json, Encoding.UTF8, "application/json") };
    }
}
