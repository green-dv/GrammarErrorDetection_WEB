using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.WEB.Views.Auth.Models;

public sealed class RegisterFormModel
{
    [Required(ErrorMessage = "Ingresa tus nombres.")]
    [StringLength(50, ErrorMessage = "Los nombres admiten hasta 50 caracteres.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tus apellidos.")]
    [StringLength(50, ErrorMessage = "Los apellidos admiten hasta 50 caracteres.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu CI.")]
    [StringLength(15, ErrorMessage = "El CI admite hasta 15 caracteres.")]
    public string CI { get; set; } = string.Empty;

    [StringLength(12, ErrorMessage = "El teléfono admite hasta 12 caracteres.")]
    [Phone(ErrorMessage = "Ingresa un teléfono válido.")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(100, ErrorMessage = "El correo admite hasta 100 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa una contraseña.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 72 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
