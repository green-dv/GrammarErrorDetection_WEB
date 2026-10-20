namespace GrammarErrorDetection.Models.Users;

// Solo se deserializan los datos del usuario; no se conservan contraseñas ni secretos TOTP.
public sealed class UserUpdateResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Ci { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool HasAuthenticator { get; set; }
    public bool IsTotpEnabled { get; set; }
    public bool ChangePassword { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
