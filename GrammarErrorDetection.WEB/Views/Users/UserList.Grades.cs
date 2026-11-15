using System.Text.Json;

namespace GrammarErrorDetection.WEB.Views.Users;

public partial class UserList
{
    private Task RetryGradesAsync() => LoadGradesAsync(lifetime.Token, requestVersion);

    private async Task LoadGradesAsync(CancellationToken cancellationToken, int version)
    {
        if (!CanRegisterGrades || Course is null) return;
        loadingGrades = true;
        gradesError = null;
        gradeDrafts.Clear();
        gradeRegistrations.Clear();
        try
        {
            // Resolver por el curso abierto, independientemente del CurrentCourse del estudiante.
            var registrations = new Dictionary<Guid, Guid>();
            var seen = new HashSet<Guid>();
            for (var registrationPage = 1; ; registrationPage++)
            {
                var response = await GradeCourseService.GetRegistrationsAsync(Course.Id, registrationPage, cancellationToken);
                if (disposed || version != requestVersion) return;
                if (response?.Data is null || response.Error is { ErrorCode: not 0 })
                    throw new InvalidOperationException("No se pudieron consultar las inscripciones de este curso.");
                var added = 0;
                foreach (var item in response.Data)
                {
                    if (item.ClassId != Course.Id || item.Id == Guid.Empty || item.UserId == Guid.Empty)
                        throw new InvalidOperationException("La API devolvió una inscripción que no corresponde al curso seleccionado.");
                    if (!seen.Add(item.Id)) continue;
                    added++;
                    if (item.Active && !registrations.TryAdd(item.UserId, item.Id))
                        throw new InvalidOperationException("Hay más de una inscripción activa del estudiante en este curso.");
                }
                if (response.Data.Count < 100) break;
                if (added == 0) throw new InvalidOperationException("La consulta de inscripciones no avanzó de página.");
            }
            foreach (var user in users.ToArray())
            {
                if (!registrations.TryGetValue(user.Id, out var registrationId)) continue;
                gradeRegistrations[user.Id] = registrationId;
                var evaluations = await EvaluationService.GetAsync(registrationId, cancellationToken);
                if (disposed || version != requestVersion) return;
                var draft = new StudentGradeDraft();
                foreach (var group in evaluations.Where(item => item.Period is 1 or 2).GroupBy(item => item.Period))
                {
                    if (group.Count() > 1) throw new InvalidOperationException("Hay evaluaciones duplicadas para un mismo examen. Revisa los registros antes de editar.");
                    draft.Set(group.Single());
                }
                gradeRegistrations[user.Id] = registrationId;
                gradeDrafts[user.Id] = draft;
            }
        }
        catch (HttpRequestException ex)
        {
            if (!disposed && version == requestVersion) gradesError = ex.StatusCode == System.Net.HttpStatusCode.Forbidden
                ? "La API rechazó la consulta de inscripciones o evaluaciones de este curso (403). Verifica que seas su docente asignado."
                : HttpError(ex);
        }
        catch (OperationCanceledException) { if (!disposed && version == requestVersion) gradesError = "La consulta de notas se canceló. Inténtalo de nuevo."; }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException)
        {
            if (!disposed && version == requestVersion) gradesError = ex is JsonException ? "No se pudieron interpretar las evaluaciones." : ex.Message;
        }
        finally { if (version == requestVersion) loadingGrades = false; }
    }
}
