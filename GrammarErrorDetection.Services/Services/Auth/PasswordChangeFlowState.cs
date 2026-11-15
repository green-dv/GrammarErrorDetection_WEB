using GrammarErrorDetection.Models.Auth;

namespace GrammarErrorDetection.Services.Services.Auth;

/// <summary>
/// Cambio obligatorio de contraseña pendiente tras verificar el acceso. Solo vive en memoria (nunca en el almacenamiento
/// del navegador) y se limpia al completarlo o cancelarlo.
/// </summary>
public sealed class PasswordChangeFlowState
{
    public PasswordChangeTicket? Ticket { get; private set; }
    public string? Email { get; private set; }
    public bool? IsTotpEnabled { get; private set; }
    public string? Name { get; private set; }

    public bool HasPending => Ticket is { } ticket && ticket.ChallengeId != Guid.Empty
        && !string.IsNullOrEmpty(ticket.Token) && ticket.ExpiresAt > DateTimeOffset.UtcNow;

    public void Start(AuthSessionResponse response, string email, bool? isTotpEnabled)
    {
        ArgumentNullException.ThrowIfNull(response);
        Ticket = response.PasswordChange ?? throw new InvalidOperationException("La API no entregó el ticket para cambiar la contraseña.");
        Email = email;
        IsTotpEnabled = isTotpEnabled;
        Name = response.User?.Name;
    }

    public CompletePasswordChangeRequest CreateRequest(string newPassword) => Ticket is { } ticket
        ? new() { ChallengeId = ticket.ChallengeId, Token = ticket.Token, NewPassword = newPassword }
        : throw new InvalidOperationException("Vuelve a iniciar sesión.");

    public void Clear()
    {
        Ticket = null;
        Email = null;
        IsTotpEnabled = null;
        Name = null;
    }
}
