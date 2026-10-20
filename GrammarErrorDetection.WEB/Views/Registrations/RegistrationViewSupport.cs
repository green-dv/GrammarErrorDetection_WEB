using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using GrammarErrorDetection.Models.Registrations;
using GrammarErrorDetection.Models.Responses;

namespace GrammarErrorDetection.WEB.Views.Registrations;

internal static class RegistrationViewSupport
{
    public static T Data<T>(ApiResponse<T>? response) where T : class
    {
        if (response?.Error is { ErrorCode: not 0 } error)
            throw new InvalidOperationException(error.ErrorMessage ?? "La operación no pudo completarse.");
        return response?.Data ?? throw new InvalidOperationException("El servicio no devolvió una respuesta válida.");
    }
    public static bool IsPending(RegistrationResponse item) => item.Active && string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase);
    public static bool IsPending(RegistrationListItem item) => item.RegistrationId is { } id && id != Guid.Empty && item.Active != false && string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase);
    public static string Status(RegistrationListItem item) => Status(new RegistrationResponse { Status = item.Status });
    public static string StatusClass(RegistrationListItem item) => StatusClass(new RegistrationResponse { Status = item.Status });
    public static string Status(RegistrationResponse item) => item.Status?.ToLowerInvariant() switch
    {
        "pending" => "Pendiente", "approved" => "Aprobada", "rejected" => "Rechazada", _ => item.Status ?? "Sin estado informado"
    };
    public static string StatusClass(RegistrationResponse item) => item.Status?.ToLowerInvariant() switch
    {
        "pending" => "pending", "approved" => "approved", "rejected" => "rejected", _ => ""
    };
    public static string Text(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
    public static string Hour(string? value) => TimeOnly.TryParse(value?.TrimEnd('Z', 'z'), CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) ? time.ToString("HH:mm") : "—";
    public static string Error(Exception ex) => ex switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } => "Tu sesión venció. Vuelve a iniciar sesión.",
        HttpRequestException { StatusCode: HttpStatusCode.Forbidden } => "No tienes permiso para realizar esta operación.",
        HttpRequestException { StatusCode: HttpStatusCode.NotFound } => "La inscripción o el curso ya no está disponible. Actualiza el listado.",
        HttpRequestException { StatusCode: HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity } => "No se pudo completar la operación. Revisa la solicitud y actualiza los datos: el estado, la elegibilidad o el cupo pueden haber cambiado.",
        HttpRequestException => "No se pudo conectar con el servicio. Inténtalo de nuevo.",
        OperationCanceledException => "La operación tardó demasiado. Actualiza el listado antes de volver a intentarlo.",
        System.Text.Json.JsonException => "No se pudo interpretar la respuesta del servicio.",
        _ => ex.Message
    };
}

public sealed class RegistrationListFilters
{
    private const string Uuid = @"^\s*[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\s*$";
    [RegularExpression(Uuid, ErrorMessage = "El ID del estudiante debe ser un UUID válido.")] public string? UserId { get; set; }
    [RegularExpression(Uuid, ErrorMessage = "El ID del módulo debe ser un UUID válido.")] public string? ModuleNameId { get; set; }
    public string? Status { get; set; }
    public DateOnly? RegistrationDate { get; set; }
    public RegistrationFilter ToFilter(bool mine, bool coursesOnly) => new()
    {
        UserId = mine ? null : Parse(UserId), ModuleNameId = Parse(ModuleNameId),
        Status = Status, Active = coursesOnly ? true : null,
        RegistrationDate = RegistrationDate
    };
    private static Guid? Parse(string? value) => string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value.Trim());
}
