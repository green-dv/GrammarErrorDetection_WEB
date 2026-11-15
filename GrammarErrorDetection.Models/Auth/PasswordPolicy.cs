using System.Text;

namespace GrammarErrorDetection.Models.Auth;

/// <summary>
/// Contraseña segura, igual que en la API: al menos 8 caracteres, una mayúscula, una minúscula, un número
/// y un carácter especial (cualquier carácter que no sea letra, número ni espacio). Máximo 72 bytes (BCrypt).
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumBytes = 72;

    /// <summary>Cada requisito con su texto, para mostrarlo como lista de verificación mientras se escribe.</summary>
    public static IReadOnlyList<(string Label, bool Met)> Check(string? password)
    {
        var value = password ?? string.Empty;
        return
        [
            ("Al menos 8 caracteres", value.Length >= MinimumLength),
            ("Una letra mayúscula", value.Any(char.IsUpper)),
            ("Una letra minúscula", value.Any(char.IsLower)),
            ("Un número", value.Any(char.IsDigit)),
            ("Un carácter especial (por ejemplo ! @ # $ %)", value.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
        ];
    }

    public static bool IsStrong(string? password)
        => Check(password).All(rule => rule.Met) && Encoding.UTF8.GetByteCount(password ?? string.Empty) <= MaximumBytes;

    /// <summary>Errores para el formulario: requisitos no cumplidos, longitud máxima y confirmación.</summary>
    public static List<string> Validate(string password, string confirmation)
    {
        var problems = Check(password).Where(rule => !rule.Met).Select(rule => $"Falta: {rule.Label.ToLowerInvariant()}.").ToList();
        if (Encoding.UTF8.GetByteCount(password) > MaximumBytes) problems.Add("La contraseña es demasiado larga.");
        if (password != confirmation) problems.Add("Las contraseñas nuevas no coinciden.");
        return problems;
    }
}

public sealed class ChangeOwnPasswordRequest
{
    /// <summary>Contraseña actual.</summary>
    public string Password { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
