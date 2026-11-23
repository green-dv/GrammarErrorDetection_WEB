using System.Net.Http.Json;
using GrammarErrorDetection.Services.Localization;
using Microsoft.JSInterop;

namespace GrammarErrorDetection.WEB.Storage;

/// <summary>Lee/guarda el idioma elegido (localStorage) y carga el diccionario inglés al arrancar.</summary>
public static class LanguageStorage
{
    private const string Key = "ceti-lang";

    public static async Task InitializeAsync(IJSRuntime js, HttpClient http)
    {
        var saved = await ReadAsync(js);
        var language = I18n.Normalize(saved);
        IReadOnlyDictionary<string, string>? dictionary = null;
        if (language == I18n.English)
        {
            try { dictionary = await http.GetFromJsonAsync<Dictionary<string, string>>("i18n/en.json"); }
            catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException) { language = I18n.Spanish; }
        }
        I18n.Configure(language, dictionary);
        GrammarErrorDetection.Models.Responses.ApiError.Localize = text => I18n.T(text);
        await js.InvokeVoidAsync("eval", $"document.documentElement.lang='{I18n.Language}'");
    }

    /// <summary>Guarda el idioma y lo aplica sin recargar la página.</summary>
    public static async Task ChangeAsync(IJSRuntime js, HttpClient http, string language)
    {
        language = I18n.Normalize(language);
        try { await js.InvokeVoidAsync("localStorage.setItem", Key, language); }
        catch (JSException) { }
        IReadOnlyDictionary<string, string>? dictionary = null;
        if (language == I18n.English)
        {
            try { dictionary = await http.GetFromJsonAsync<Dictionary<string, string>>("i18n/en.json"); }
            catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException) { language = I18n.Spanish; }
        }
        await js.InvokeVoidAsync("eval", $"document.documentElement.lang='{language}'");
        I18n.Configure(language, dictionary);
    }

    private static async Task<string?> ReadAsync(IJSRuntime js)
    {
        try { return await js.InvokeAsync<string?>("localStorage.getItem", Key); }
        catch (JSException) { return null; }
    }
}
