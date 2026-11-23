using System.Globalization;

namespace GrammarErrorDetection.Services.Localization;

/// <summary>
/// Traducción de la interfaz. El texto en español es la clave: si no hay entrada en el diccionario
/// del idioma activo (o el idioma es español), se muestra el propio texto.
/// El estado es estático porque Blazor WebAssembly atiende a un único usuario por pestaña.
/// </summary>
public static class I18n
{
    public const string Spanish = "es";
    public const string English = "en";
    public static readonly IReadOnlyList<string> Supported = [Spanish, English];

    private static IReadOnlyDictionary<string, string> english = new Dictionary<string, string>();

    /// <summary>Se dispara tras cambiar de idioma para que la interfaz se vuelva a dibujar.</summary>
    public static event Action? Changed;

    public static string Language { get; private set; } = Spanish;
    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("es-ES");

    /// <summary>Cultura para fechas y números de Bolivia (español) o EE. UU. (inglés).</summary>
    public static CultureInfo BoliviaCulture { get; private set; } = CultureInfo.GetCultureInfo("es-BO");

    public static string Normalize(string? language) =>
        language is not null && language.StartsWith(English, StringComparison.OrdinalIgnoreCase) ? English : Spanish;

    /// <summary>Activa el idioma y, para inglés, el diccionario español → inglés.</summary>
    public static void Configure(string? language, IReadOnlyDictionary<string, string>? englishDictionary)
    {
        Language = Normalize(language);
        english = englishDictionary ?? new Dictionary<string, string>();
        Culture = CultureInfo.GetCultureInfo(Language == English ? "en-US" : "es-ES");
        BoliviaCulture = CultureInfo.GetCultureInfo(Language == English ? "en-US" : "es-BO");
        Changed?.Invoke();
    }

    public static string T(string text)
    {
        if (Language == English && english.TryGetValue(text, out var translated)) return translated;
        return text;
    }

    public static string T(string text, params object?[] args) => string.Format(Culture, T(text), args);
}
