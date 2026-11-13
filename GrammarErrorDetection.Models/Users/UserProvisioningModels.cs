using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Users;

/// <summary>Alta por el coordinador: sin contraseña; la API genera una temporal y la envía al correo.</summary>
public sealed class UserCreateRequest
{
    [Required(ErrorMessage = "Ingresa el nombre."), StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el apellido."), StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el documento de identidad."), StringLength(15)]
    public string CI { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el correo."), StringLength(100), EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(12), Phone(ErrorMessage = "El teléfono no es válido.")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Selecciona el rol.")]
    public Guid? RoleId { get; set; }

    /// <summary>Opcional: si se deja vacía se guarda como nula.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("fechaNacimiento")]
    public DateOnly? BirthDate { get; set; }
}

public sealed class UserCredentialsResult
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool CredentialsSent { get; set; }
    public string Message { get; set; } = string.Empty;
}
