using GrammarErrorDetection.Models.Evaluations;

namespace GrammarErrorDetection.WEB.Views.Users;

// Borrador separado de las evaluaciones persistidas.
public sealed class StudentGradeDraft
{
    public Guid?[] Ids { get; } = new Guid?[2];
    public decimal?[][] Exams { get; } = [new decimal?[6], new decimal?[6]];
    public string[] Comments { get; } = ["", ""];
    public const int CommentMaxLength = 500;
    public decimal? Total(int exam) => Exams[exam].All(value => value.HasValue)
        ? Exams[exam].Sum(value => value!.Value) : null;
    public decimal? Average => Total(0) is { } first && Total(1) is { } second ? (first + second) / 2m : null;
    public StudentGradeDraft Copy()
    {
        var copy = new StudentGradeDraft();
        Ids.CopyTo(copy.Ids, 0);
        for (var exam = 0; exam < 2; exam++) Exams[exam].CopyTo(copy.Exams[exam], 0);
        Comments.CopyTo(copy.Comments, 0);
        return copy;
    }
    public void Set(EvaluationResponse value)
    {
        if (value.Period is not (1 or 2)) return;
        var exam = value.Period.Value - 1;
        Ids[exam] = value.Id;
        Exams[exam] = [value.WritingScore, value.OralScore, value.ListeningScore,
            value.ReadingScore, GrammarAndVocabulary(value), value.AcademicPerformance];
        Comments[exam] = value.Comment ?? "";
    }
    public EvaluationWriteRequest Request(Guid registrationId, int exam) => new()
    {
        RegistrationId = registrationId, Period = exam + 1,
        WritingScore = (int?)Exams[exam][0], OralScore = (int?)Exams[exam][1],
        ListeningScore = (int?)Exams[exam][2], ReadingScore = (int?)Exams[exam][3],
        // El formulario captura gramática y vocabulario como un solo puntaje (máximo 10 compartido): va en GrammarScore
        // y VocabularyScore se fija en 0 para que no sobreviva un valor anterior y la API cuente la evaluación completa.
        GrammarScore = (int?)Exams[exam][4], VocabularyScore = Exams[exam][4] is null ? null : 0,
        AcademicPerformance = (int?)Exams[exam][5],
        // Siempre se envía (vacío = sin comentario) para que borrar el texto también borre el guardado.
        Comment = Comments[exam].Trim()
    };
    private static decimal? GrammarAndVocabulary(EvaluationResponse value)
        => value.GrammarScore is null ? null : value.GrammarScore + (value.VocabularyScore ?? 0);
    /// <summary>Error del comentario o null si es válido (texto libre, sin caracteres de control salvo saltos de línea).</summary>
    public static string? CommentError(string? comment)
    {
        var text = comment?.Trim() ?? "";
        if (text.Length > CommentMaxLength) return I18n.T("El comentario admite hasta {0} caracteres.", CommentMaxLength);
        return text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')) ? I18n.T("El comentario contiene caracteres no permitidos.") : null;
    }
    public static string Result(decimal? score) => score is null ? I18n.T("Pendiente") : score >= 70 ? I18n.T("Aprobado") : I18n.T("Reprobado");
    public static string Color(decimal? score) => score is null ? "" : score < 70m ? "grade-below" : "grade-passed";
}
