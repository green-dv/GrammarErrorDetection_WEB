using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Users;

public sealed class UserSearchRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserSearchFilter? Filter { get; set; }
}

public sealed record UserSearchFilter
{
    [StringLength(50, ErrorMessage = "El nombre admite hasta 50 caracteres.")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FirstName { get; set; }
    [StringLength(50, ErrorMessage = "El apellido admite hasta 50 caracteres.")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastName { get; set; }
    [StringLength(15, ErrorMessage = "El CI admite hasta 15 caracteres.")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ci { get; set; }
    [StringLength(12, ErrorMessage = "El teléfono admite hasta 12 caracteres.")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Phone { get; set; }
    [StringLength(100, ErrorMessage = "El correo admite hasta 100 caracteres.")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? CreatedAt { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? RoleId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Active { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CurrentModuleNameId { get; set; }

    public UserSearchFilter? Normalize()
    {
        var result = this with { FirstName = Trim(FirstName), LastName = Trim(LastName), Ci = Trim(Ci), Phone = Trim(Phone), Email = Trim(Email) };
        return result == new UserSearchFilter() ? null : result;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
