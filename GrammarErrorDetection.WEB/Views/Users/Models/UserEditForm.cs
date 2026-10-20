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
    public Guid RoleId { get; set; }
    public bool Active { get; set; }
    public bool ChangePassword { get; set; }

    public static UserEditForm From(UserSearchResponse user) => new() { FirstName = user.FirstName, LastName = user.LastName, Ci = user.Ci, Email = user.Email, Phone = user.Phone, RoleId = user.RoleId, Active = user.Active, ChangePassword = user.ChangePassword };

    public UserUpdateRequest ChangesFrom(UserSearchResponse original) => new()
    {
        Id = original.Id,
        FirstName = Changed(FirstName, original.FirstName), LastName = Changed(LastName, original.LastName),
        Ci = Changed(Ci, original.Ci), Email = Changed(Email, original.Email), Phone = Changed(Phone, original.Phone),
        RoleId = RoleId != original.RoleId ? RoleId : null,
        Active = Active != original.Active ? Active : null,
        ChangePassword = ChangePassword != original.ChangePassword ? ChangePassword : null
    };

    private static string? Changed(string? value, string? original) => string.Equals(value?.Trim() ?? "", original?.Trim() ?? "", StringComparison.Ordinal) ? null : value?.Trim() ?? "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RoleId == Guid.Empty) yield return new ValidationResult("Selecciona un rol.", [nameof(RoleId)]);
    }
}
