namespace GrammarErrorDetection.Services.Services.Registrations;

/// <summary>
/// Invitación pendiente mientras el visitante se registra o inicia sesión. Solo en memoria (igual que el resto de
/// estados de registro): el token no es una credencial, pero tampoco se persiste en el navegador.
/// </summary>
public sealed class CourseInvitationFlowState
{
    public string? PendingToken { get; private set; }
    public void Remember(string token) => PendingToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
    public void Clear() => PendingToken = null;
}
