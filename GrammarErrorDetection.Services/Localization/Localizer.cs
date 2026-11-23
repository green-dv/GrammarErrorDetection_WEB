namespace GrammarErrorDetection.Services.Localization;

/// <summary>Acceso a las traducciones desde los componentes: <c>@inject Localizer L</c> y <c>@L["Texto"]</c>.</summary>
public sealed class Localizer
{
    public string Language => I18n.Language;
    public System.Globalization.CultureInfo Culture => I18n.Culture;
    public bool IsEnglish => I18n.Language == I18n.English;

    public string this[string text] => I18n.T(text);
    public string this[string text, params object?[] args] => I18n.T(text, args);
}
