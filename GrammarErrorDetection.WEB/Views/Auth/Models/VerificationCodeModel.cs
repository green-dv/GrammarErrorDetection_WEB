using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.WEB.Views.Auth.Models;

public sealed class VerificationCodeModel
{
    [Required(ErrorMessage = "Ingresa el código de verificación.")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "El código debe tener seis dígitos.")]
    public string Code { get; set; } = string.Empty;
}
