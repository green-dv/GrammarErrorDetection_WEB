using System.Net;
using GrammarErrorDetection.Infraestructure.ApiClients;

namespace GrammarErrorDetection.Services.Localization;

/// <summary>
/// Texto para el usuario a partir de un error de la API. Regla única para toda la interfaz:
/// si el servidor envió un mensaje concreto (validación, permisos, recurso inexistente, conflicto…) se muestra tal cual;
/// ante cualquier error desconocido se dice solo que ocurrió un error interno del servidor.
/// </summary>
public static class ApiErrors
{
    /// <summary>Mensaje del servidor (traducido si existe en en.json) cuando es un error concreto del cliente; si no, null.</summary>
    public static string? ServerMessage(Exception exception)
    {
        if (exception is not ApiException { ServerMessage: { } message } api) return null;
        var status = (int?)api.StatusCode ?? 0;
        // 4xx: el servidor explica qué pasó. 502/503/504: el servicio externo o el tiempo de espera fallaron, y también se explican.
        // 500 y el resto se consideran errores internos: su texto no sirve al usuario.
        return status is >= 400 and < 500 or 502 or 503 or 504 ? I18n.T(message) : null;
    }

    /// <summary>Mensaje final para mostrar. <paramref name="exception"/> suele ser un <see cref="HttpRequestException"/>.</summary>
    public static string Describe(Exception exception)
    {
        if (ServerMessage(exception) is { } message) return message;
        // Sin código de estado no hubo respuesta: red caída, CORS o servidor apagado.
        if (exception is HttpRequestException { StatusCode: null } and not ApiException)
            return I18n.T("No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.");
        return I18n.T("Ocurrió un error interno del servidor.");
    }

    /// <summary>Atajo para los <c>switch</c> por código de estado: mensaje del servidor y, si no hay, el contextual.</summary>
    public static string DescribeOr(Exception exception, string contextual) =>
        ServerMessage(exception) ?? contextual;

    /// <summary>Error real del servidor seguido de un consejo, para operaciones de resultado incierto (p. ej. revisar el listado antes de reintentar).</summary>
    public static string WithAdvice(Exception exception, string advice) => Describe(exception) + " " + I18n.T(advice);

    public static bool IsUnauthorized(Exception exception) =>
        exception is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized };
}
