namespace GrammarErrorDetection.WEB.Navigation;

public sealed record PortalMenuOption(string Path, string Label, string Icon, string[] Roles, string Keywords = "");
public sealed record PortalMenuSection(string Id, string Label, string[] Roles, string[] Paths);

public static class PortalNavigation
{
    public static IReadOnlyList<PortalMenuSection> Sections { get; } = [
        new("community", "Comunidad", [], ["home", "leaderboard"]),
        new("learning", "Aprendizaje", [], ["materials", "recommendations", "process", "gamification"]),
        new("my-learning", "Mi formación", ["Student"], ["my-courses", "my-enrollments"]),
        new("academic", "Gestión académica", ["Coordinator", "Teacher"], ["courses", "enrollments", "reports"]),
        new("administration", "Administración", ["Coordinator"], ["users"])
    ];

    public static bool CanViewSection(PortalMenuSection section, IReadOnlyList<string> roles) =>
        (section.Roles.Length == 0 || section.Roles.Any(role => roles.Contains(role, StringComparer.Ordinal))) &&
        section.Paths.Any(path => CanAccess(path, roles));

    private const string Book = "<path d='M12 5v15M3 4h5a4 4 0 0 1 4 2 4 4 0 0 1 4-2h5v15h-5a4 4 0 0 0-4 2 4 4 0 0 0-4-2H3Z'/>";
    private const string List = "<path d='M9 4H5v17h14V4h-4M9 3h6v4H9ZM8 12h8m-8 4h5'/>";
    public static IReadOnlyList<PortalMenuOption> Options { get; } = [
        new("home", "Inicio", "<path d='m3 10 9-7 9 7v10a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1Z'/>", []),
        new("materials", "Material de apoyo", "<path d='M3 7h7l2 2h9v11H3ZM3 7V4h7l2 3h9v2'/><path d='M8 13h8m-8 3h5'/>", []),
        new("recommendations", "Recomendaciones", "<path d='M9 18h6m-5 3h4M12 3a6 6 0 0 0-3.5 10.9c.6.5 1 1.2 1 2.1h5c0-.9.4-1.6 1-2.1A6 6 0 0 0 12 3Z'/>", ["Student"], "material recomendado errores refuerzo sugerencias"),
        new("leaderboard", "Leaderboard", "<path d='M8 3h8v7a4 4 0 0 1-8 0ZM8 5H4v3a4 4 0 0 0 4 4m8-7h4v3a4 4 0 0 1-4 4m-4 2v6m-4 1h8'/>", []),
        new("process", "Procesar", "<path d='M4 4h16v16H4ZM8 8h8m-8 4h8m-8 4h4'/>", [], "oraciones párrafos análisis"),
        new("my-enrollments", "Mis inscripciones", List, ["Student"]),
        new("my-courses", "Mis cursos", Book, ["Student"]),
        new("users", "Usuarios", "<circle cx='9' cy='8' r='3'/><path d='M3 20v-2a6 6 0 0 1 12 0v2M16 5a3 3 0 0 1 0 6m2 3a5 5 0 0 1 3 4v2'/>", ["Coordinator"]),
        new("courses", "Cursos", Book, ["Coordinator", "Teacher"]),
        new("reports", "Reportes", "<path d='M4 20h16M7 16v-5m5 5V5m5 11V8'/>", ["Coordinator", "Teacher"], "gramática desempeño falencias estadísticas"),
        new("enrollments", "Inscripciones", List, ["Coordinator"]),
        new("gamification", "Gamificación", "<path d='M8 3h8v7a4 4 0 0 1-8 0ZM8 5H4v3a4 4 0 0 0 4 4m8-7h4v3a4 4 0 0 1-4 4m-4 2v6m-4 1h8'/>", ["Coordinator", "Teacher", "Student"], "logros desafíos challenges")
    ];

    public static bool IsAuthPath(string path) => path is "" or "login" or "register" || path.StartsWith("auth/", StringComparison.OrdinalIgnoreCase);
    public static bool CanAccess(string path, IReadOnlyList<string> roles)
    {
        var segments = path.Trim('/').Split('/');
        if (segments.Length == 2 && segments[0].Equals("users", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(segments[1], out _))
            return true;
        var section = path.Split('/')[0];
        if (section.Equals("teaching-courses", StringComparison.OrdinalIgnoreCase)) section = "courses";
        var option = Options.FirstOrDefault(item => item.Path.Equals(section, StringComparison.OrdinalIgnoreCase));
        return option is null || option.Roles.Length == 0 || option.Roles.Any(role => roles.Contains(role, StringComparer.Ordinal));
    }
}
