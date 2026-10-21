using GrammarErrorDetection.Models.Evaluations;

namespace GrammarErrorDetection.WEB.Views.Users;

// Borrador separado de las evaluaciones persistidas.
public sealed class StudentGradeDraft
{
    public Guid?[] Ids { get; } = new Guid?[2];
    public decimal?[][] Exams { get; } = [new decimal?[6], new decimal?[6]];
    public decimal? Total(int exam) => Exams[exam].All(value => value.HasValue)
        ? Exams[exam].Sum(value => value!.Value) : null;
    public decimal? Average => Total(0) is { } first && Total(1) is { } second ? (first + second) / 2m : null;
    public StudentGradeDraft Copy()
    {
        var copy = new StudentGradeDraft();
        Ids.CopyTo(copy.Ids, 0);
        for (var exam = 0; exam < 2; exam++) Exams[exam].CopyTo(copy.Exams[exam], 0);
        return copy;
    }
    public void Set(EvaluationResponse value)
    {
        if (value.Period is not (1 or 2)) return;
        var exam = value.Period.Value - 1;
        Ids[exam] = value.Id;
        Exams[exam] = [value.WritingScore, value.OralScore, value.ListeningScore,
            value.ReadingScore, value.GrammarScore, value.AcademicPerformance];
    }
    public EvaluationWriteRequest Request(Guid registrationId, int exam) => new()
    {
        RegistrationId = registrationId, Period = exam + 1,
        WritingScore = (int?)Exams[exam][0], OralScore = (int?)Exams[exam][1],
        ListeningScore = (int?)Exams[exam][2], ReadingScore = (int?)Exams[exam][3],
        GrammarScore = (int?)Exams[exam][4], AcademicPerformance = (int?)Exams[exam][5]
    };
    public static string Result(decimal? score) => score is null ? "Pendiente" : score >= 70 ? "Aprobado" : "Reprobado";
    public static string Color(decimal? score) => score is null ? "" : score < 70m ? "grade-below" : "grade-passed";
}
