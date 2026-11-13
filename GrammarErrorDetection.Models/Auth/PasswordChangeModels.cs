namespace GrammarErrorDetection.Models.Auth;

/// <summary>Ticket de un solo uso que entrega la API cuando la cuenta debe cambiar su contraseña antes del JWT.</summary>
public sealed class PasswordChangeTicket
{
    public Guid ChallengeId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class CompletePasswordChangeRequest
{
    public Guid ChallengeId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
