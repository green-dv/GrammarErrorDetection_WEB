using System.Globalization;

namespace GrammarErrorDetection.WEB.Views.Courses;

internal static class CourseDisplay
{
    public static string Text(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
    public static string Hour(string? value) => TimeOnly.TryParse(value?.TrimEnd('Z', 'z'), CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) ? time.ToString("HH:mm") : "—";
}
