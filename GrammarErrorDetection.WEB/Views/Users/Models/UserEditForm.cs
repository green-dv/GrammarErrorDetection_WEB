using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Users;

namespace GrammarErrorDetection.WEB.Views.Users.Models;

public sealed class UserEditForm : IValidatableObject
{
    [Required(ErrorMessage = "Ingresa el nombre."), StringLength(50, ErrorMessage = "El nombre admite hasta 50 caracteres.")]
    public string FirstName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresa el apellido."), StringLength(50, ErrorMessage = "El apellido admite hasta 50 caracteres.")]
    public string LastName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresa el CI."), StringLength(15, ErrorMessage = "El CI admite hasta 15 caracteres.")]
    public string Ci { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresa el correo."), EmailAddress(ErrorMessage = "Ingresa un correo válido."), StringLength(100)]
    public string Email { get; set; } = string.Empty;
    [StringLength(12, ErrorMessage = "El teléfono admite hasta 12 caracteres."), RegularExpression(@"^\+?[\d\s()-]*$", ErrorMessage = "Ingresa un teléfono válido.")]
    public string? Phone { get; set; }
    public DateOnly? BirthDate { get; set; }

    public static UserEditForm From(UserSearchResponse user) => new() { FirstName = user.FirstName, LastName = user.LastName, Ci = user.Ci, Email = user.Email, Phone = user.Phone, BirthDate = user.BirthDate };

    public bool HasChangesFrom(UserSearchResponse original) => Changed(FirstName, original.FirstName) is not null
        || Changed(LastName, original.LastName) is not null || Changed(Ci, original.Ci) is not null
        || Changed(Email, original.Email) is not null || Changed(Phone, original.Phone) is not null
        || BirthDate != original.BirthDate;

    public UserDetailsUpdateRequest ToRequest(Guid id) => new()
    {
        Id = id,
        FirstName = FirstName.Trim(), LastName = LastName.Trim(),
        Ci = Ci.Trim(), Email = Email.Trim(), Phone = Phone?.Trim(), BirthDate = BirthDate
    };

    private static string? Changed(string? value, string? original) => string.Equals(value?.Trim() ?? "", original?.Trim() ?? "", StringComparison.Ordinal) ? null : value?.Trim() ?? "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BirthDate > DateOnly.FromDateTime(DateTime.Today))
            yield return new ValidationResult("La fecha de nacimiento no puede ser futura.", [nameof(BirthDate)]);
    }
}
