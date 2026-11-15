using System.Text.Json;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.JSInterop;

namespace GrammarErrorDetection.WEB.Storage;

public sealed class BrowserAuthSessionStorage(IJSRuntime jsRuntime) : IAuthSessionStorage
{
    private const string StorageKey = "ceti.auth.session";
    private readonly IJSInProcessRuntime browser = (IJSInProcessRuntime)jsRuntime;

    public AuthSessionSnapshot? Read()
    {
        try
        {
            var json = browser.Invoke<string?>("sessionStorage.getItem", StorageKey);
            return json is null ? null : JsonSerializer.Deserialize<AuthSessionSnapshot>(json);
        }
        catch (JsonException)
        {
            Remove();
            return null;
        }
        catch (JSException)
        {
            return null;
        }
    }

    public void Write(AuthSessionSnapshot snapshot)
    {
        try { browser.InvokeVoid("sessionStorage.setItem", StorageKey, JsonSerializer.Serialize(snapshot)); }
        catch (JSException) { /* Si el navegador bloquea el almacenamiento, se conserva la sesión en memoria. */ }
    }

    public void Remove()
    {
        try { browser.InvokeVoid("sessionStorage.removeItem", StorageKey); }
        catch (JSException) { /* El almacenamiento puede estar deshabilitado por el navegador. */ }
    }
}
