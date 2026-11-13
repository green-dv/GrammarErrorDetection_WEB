namespace GrammarErrorDetection.WEB.Views.Gamification.Badges;

public static class BadgeCatalog
{
    public static IReadOnlyList<BadgeAsset> Centers { get; } =
    [
        new("circle", "Círculo", "images/centers/Plain%20Badge%20Template%202.png"),
        new("ribbon-circle", "Círculo con cintas", "images/centers/Plain%20Badge%20Template.png")
    ];
    // Los archivos se asignan por su función, independientemente del nombre de la carpeta.
    public static IReadOnlyList<BadgeAsset> Icons { get; } =
    [
        new("calendar", "Calendario", "images/tops/Calendar%201.png"),
        new("clipboard-one", "Lista de tareas", "images/tops/Clipboard%201.png"),
        new("clipboard-two", "Lista completada", "images/tops/Clipboard%202.png"),
        new("fire", "Fuego", "images/tops/Fire%201.png"),
        new("target", "Objetivo", "images/tops/Target.png")
    ];
    public static IReadOnlyList<BadgeAsset> Tops { get; } =
    [
        new("one-star", "Una estrella", "images/icons/1%20Star.png", 175, 169, 175),
        new("two-stars", "Dos estrellas", "images/icons/2%20Star.png", 312, 136, 136),
        new("three-stars", "Tres estrellas", "images/icons/3%20Star.png", 398, 202, 136),
        new("four-stars", "Cuatro estrellas", "images/icons/4%20Star.png", 389, 221, 110),
        new("five-stars", "Cinco estrellas", "images/icons/5%20Star.png", 422, 274, 110)
    ];
    public static IReadOnlyList<string> Colors { get; } =
        ["#FFFFFF", "#D6A744", "#B57946", "#B7C4D3", "#5B9BCE", "#5EAE98", "#9A7CC1", "#D77C74"];
    public static IEnumerable<BadgeAsset> SelectedAssets(BadgeDesign design)
    {
        yield return Centers.Single(a => a.Id == design.CenterId);
        if (design.IconId is not null) yield return Icons.Single(a => a.Id == design.IconId);
        if (design.TopId is not null) yield return Tops.Single(a => a.Id == design.TopId);
    }
}
