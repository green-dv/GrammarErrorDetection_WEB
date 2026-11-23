using System.Net;

namespace GrammarErrorDetection.Infraestructure.ApiClients;

/// <summary>
/// Respuesta de error HTTP de la API. Conserva el mensaje concreto que envía el servidor
/// (<c>error.errorMessage</c>) además del código de estado, para que la interfaz pueda mostrarlo.
/// Hereda de <see cref="HttpRequestException"/>, así que los <c>catch</c> existentes siguen funcionando.
/// </summary>
public sealed class ApiException(string message, HttpStatusCode statusCode, string? serverMessage, string? serverDetails)
    : HttpRequestException(message, null, statusCode)
{
    /// <summary>Mensaje del servidor tal como llegó (español); null si la respuesta no traía uno legible.</summary>
    public string? ServerMessage { get; } = string.IsNullOrWhiteSpace(serverMessage) ? null : serverMessage.Trim();

    /// <summary>Detalle técnico opcional del servidor (solo para diagnóstico, no se muestra al usuario).</summary>
    public string? ServerDetails { get; } = serverDetails;
}
