using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Users;

// Datos personales que puede editar un coordinador.
public sealed class UserDetailsUpdateRequest
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Ci { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    [JsonPropertyName("fechaNacimiento")]
    public DateOnly? BirthDate { get; set; }
}
