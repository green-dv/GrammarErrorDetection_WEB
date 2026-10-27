using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using GrammarErrorDetection.Infraestructure.ApiClients;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Services.Services.Users;
using GrammarErrorDetection.WEB.Views.Users.Models;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var user = JsonSerializer.Deserialize<UserSearchResponse>(
    """{"id":"a5a4f266-f082-4b35-8e26-41ad308bff20","firstName":"Juan","lastName":"Pérez","ci":"12345678","email":"juan@example.com","phone":"76543210","fechaNacimiento":"2000-05-17"}""", jsonOptions)!;
var form = UserEditForm.From(user);
Check(form.BirthDate == new DateOnly(2000, 5, 17), "La búsqueda debe leer la fecha sin hora.");
Check(!form.HasChangesFrom(user), "Abrir el formulario no debe habilitar Guardar.");
form.FirstName = " Juan ";
Check(!form.HasChangesFrom(user), "Los espacios sin cambios de contenido no deben marcar el formulario.");
form.BirthDate = new DateOnly(2001, 6, 18);
Check(form.HasChangesFrom(user), "Cambiar solo la fecha debe habilitar Guardar.");
var handler = new CaptureHandler();
var service = new UserService(new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5091/") }));
await service.UpdateDetailsAsync(form.ToRequest(user.Id));
Check(handler.Method == HttpMethod.Put && handler.Uri?.AbsolutePath == "/api/User", "La actualización debe usar PUT api/User.");
using (var document = JsonDocument.Parse(handler.Body!))
{
    var body = document.RootElement;
    Check(body.EnumerateObject().Select(p => p.Name).Order().SequenceEqual(
        new[] { "id", "firstName", "lastName", "ci", "email", "phone", "fechaNacimiento" }.Order()),
        "La edición personal debe enviar exclusivamente los campos permitidos.");
    Check(body.GetProperty("fechaNacimiento").GetString() == "2001-06-18", "La fecha debe enviarse como yyyy-MM-dd, sin hora.");
    Check(body.GetProperty("firstName").GetString() == "Juan", "La solicitud debe normalizar espacios.");
}
form.BirthDate = null;
Check(form.HasChangesFrom(user), "Borrar la fecha existente debe detectarse como cambio.");
using (var document = JsonDocument.Parse(JsonSerializer.Serialize(form.ToRequest(user.Id), jsonOptions)))
    Check(document.RootElement.GetProperty("fechaNacimiento").ValueKind == JsonValueKind.Null, "La fecha borrada debe enviarse explícitamente como null.");
form.BirthDate = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
var errors = new List<ValidationResult>();
Check(!Validator.TryValidateObject(form, new ValidationContext(form), errors, true)
    && errors.Any(error => error.MemberNames.Contains(nameof(form.BirthDate))), "Debe rechazarse una fecha de nacimiento futura.");
Console.WriteLine("UserEditChecks: todos los controles pasaron.");

sealed class CaptureHandler : HttpMessageHandler
{
    public HttpMethod? Method;
    public Uri? Uri;
    public string? Body;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Method = request.Method;
        Uri = request.RequestUri;
        Body = await request.Content!.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
    }
}
