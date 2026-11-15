using System.Security.Claims;
using System.Text.Json;

namespace GrammarErrorDetection.Services.Services.Auth;

// Lee claims solo para mostrarlos; no valida firmas ni concede permisos.
public static class JwtRoleReader
{
    public static IReadOnlyList<string> ReadRoles(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) return [];
        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (document.RootElement.ValueKind != JsonValueKind.Object) return [];
            var roles = new List<string>();
            foreach (var claim in new[] { "role", "roles", ClaimTypes.Role })
            {
                if (!document.RootElement.TryGetProperty(claim, out var value)) continue;
                if (value.ValueKind == JsonValueKind.String) roles.Add(value.GetString()!);
                else if (value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in value.EnumerateArray())
                        if (item.ValueKind == JsonValueKind.String) roles.Add(item.GetString()!);
                }
            }
            return roles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.Ordinal).ToArray();
        }
        catch (FormatException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
