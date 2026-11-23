namespace GrammarErrorDetection.Services.Localization;

/// <summary>
/// Acceso estático a <see cref="Localizer"/> para los componentes Razor (<c>@using static</c> en <c>_Imports.razor</c>):
/// permite escribir <c>@L["Texto"]</c> sin inyección de dependencias, de modo que cualquier host de pruebas renderice los componentes.
/// </summary>
public static class LocalizerAccess
{
    public static Localizer L { get; } = new();
}
