namespace GrammarErrorDetection.WEB.Navigation;

public sealed record PortalMenuOption(string Path, string Label, string Icon, string[] Roles, string Keywords = "");
public sealed record PortalMenuSection(string Id, string Label, string[] Roles, string[] Paths);

public static class PortalNavigation
{
    private static string builtFor = "";
    private static IReadOnlyList<PortalMenuSection> sections = [];
    private static IReadOnlyList<PortalMenuOption> options = [];
    private static void Ensure() { if (builtFor == I18n.Language) return; builtFor = I18n.Language; sections = BuildSections(); options = BuildOptions(); }
    public static IReadOnlyList<PortalMenuSection> Sections { get { Ensure(); return sections; } }
    public static IReadOnlyList<PortalMenuOption> Options { get { Ensure(); return options; } }

    private static IReadOnlyList<PortalMenuSection> BuildSections() => [
        new("community", I18n.T("Comunidad"), [], ["home", "leaderboard"]),
        new("learning", I18n.T("Aprendizaje"), [], ["materials", "recommendations", "process", "gamification"]),
        new("my-learning", I18n.T("Mi formación"), ["Student"], ["my-courses", "my-enrollments"]),
        new("academic", I18n.T("Gestión académica"), ["Coordinator", "Teacher"], ["courses", "enrollments", "reports"]),
        new("administration", I18n.T("Administración"), ["Coordinator"], ["users", "categories", "modules", "classrooms", "user-levels", "condition-types"])
    ];

    public static bool CanViewSection(PortalMenuSection section, IReadOnlyList<string> roles) =>
        (section.Roles.Length == 0 || section.Roles.Any(role => roles.Contains(role, StringComparer.Ordinal))) &&
        section.Paths.Any(path => CanAccess(path, roles));

    private const string Book = "<path d='M12 5v15M3 4h5a4 4 0 0 1 4 2 4 4 0 0 1 4-2h5v15h-5a4 4 0 0 0-4 2 4 4 0 0 0-4-2H3Z'/>";
    private const string List = "<path d='M9 4H5v17h14V4h-4M9 3h6v4H9ZM8 12h8m-8 4h5'/>";
    private static IReadOnlyList<PortalMenuOption> BuildOptions() => [
        new("home", I18n.T("Inicio"), "<path d='m3 10 9-7 9 7v10a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1Z'/>", []),
        new("materials", I18n.T("Material de apoyo"), "<path d='M3 7h7l2 2h9v11H3ZM3 7V4h7l2 3h9v2'/><path d='M8 13h8m-8 3h5'/>", []),
        new("recommendations", I18n.T("Recomendaciones"), "<path d='M9 18h6m-5 3h4M12 3a6 6 0 0 0-3.5 10.9c.6.5 1 1.2 1 2.1h5c0-.9.4-1.6 1-2.1A6 6 0 0 0 12 3Z'/>", ["Student"], I18n.T("material recomendado errores refuerzo sugerencias")),
        new("leaderboard", "Leaderboard", "<path d='M8 3h8v7a4 4 0 0 1-8 0ZM8 5H4v3a4 4 0 0 0 4 4m8-7h4v3a4 4 0 0 1-4 4m-4 2v6m-4 1h8'/>", []),
        new("process", I18n.T("Procesar"), "<path d='M4 4h16v16H4ZM8 8h8m-8 4h8m-8 4h4'/>", [], I18n.T("oraciones párrafos análisis")),
        new("my-enrollments", I18n.T("Mis inscripciones"), List, ["Student"]),
        new("my-courses", I18n.T("Mis cursos"), Book, ["Student"]),
        new("users", I18n.T("Usuarios"), "<circle cx='9' cy='8' r='3'/><path d='M3 20v-2a6 6 0 0 1 12 0v2M16 5a3 3 0 0 1 0 6m2 3a5 5 0 0 1 3 4v2'/>", ["Coordinator"]),
        new("categories", I18n.T("Categorías de error"), "<path d='M3 12V4h8l10 10-8 8ZM7.5 8.5h.01'/>", ["Coordinator"], I18n.T("clasificación errores gramática etiquetas reentrenamiento")),
        new("modules", I18n.T("Módulos"), "<path d='M12 3 3 8l9 5 9-5ZM3 13l9 5 9-5M3 17.5 12 22l9-4.5'/>", ["Coordinator"], I18n.T("niveles ruta cursos ciclo promoción")),
        new("classrooms", I18n.T("Aulas"), "<path d='M3 21V8l9-5 9 5v13M9 21v-6h6v6M3 21h18'/>", ["Coordinator"], I18n.T("capacidad salones espacios pisos")),
        new("user-levels", I18n.T("Niveles de puntos"), "<path d='M4 20h4v-5H4ZM10 20h4V10h-4ZM16 20h4V4h-4Z'/>", ["Coordinator"], I18n.T("gamificación puntos rangos escalera")),
        new("condition-types", I18n.T("Tipos de condición"), "<path d='M4 6h10M4 12h6M4 18h10M17 4l3 3-3 3M17 14l3 3-3 3'/>", ["Coordinator"], I18n.T("gamificación logros desafíos condiciones")),
        new("courses", I18n.T("Cursos"), Book, ["Coordinator", "Teacher"]),
        new("reports", I18n.T("Reportes"), "<path d='M4 20h16M7 16v-5m5 5V5m5 11V8'/>", ["Coordinator", "Teacher"], I18n.T("gramática desempeño falencias estadísticas")),
        new("enrollments", I18n.T("Inscripciones"), List, ["Coordinator"]),
        new("gamification", I18n.T("Gamificación"), "<path d='M8 3h8v7a4 4 0 0 1-8 0ZM8 5H4v3a4 4 0 0 0 4 4m8-7h4v3a4 4 0 0 1-4 4m-4 2v6m-4 1h8'/>", ["Coordinator", "Teacher", "Student"], I18n.T("logros desafíos challenges"))
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
