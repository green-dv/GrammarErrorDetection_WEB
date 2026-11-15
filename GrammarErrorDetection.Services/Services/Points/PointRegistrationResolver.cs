using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Points;

public sealed record PointRegistrationTarget(Guid RegistrationId, Guid ClassId, string CourseName);

public sealed class PointRegistrationResolver(ICourseService courses, IRegistrationService registrations)
{
    public async Task<PointRegistrationTarget> ResolveAsync(Guid userId, Guid? classId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || classId == Guid.Empty) throw new ArgumentException("El usuario o curso no es válido.");
        if (classId is { } courseId)
        {
            var id = await FindInCourseAsync(userId, courseId, cancellationToken);
            return id is { } found ? new(found, courseId, "")
                : throw new InvalidOperationException("No se encontró una inscripción activa de este usuario en el curso seleccionado.");
        }

        // CurrentCourse puede faltar o no incluir registrationId. Consultar las
        // inscripciones del usuario antes de concluir que no está inscrito.
        var candidates = new Dictionary<Guid, string>();
        var seen = new HashSet<Guid>();
        for (var page = 1; ; page++)
        {
            var response = await registrations.SearchAsync(new() { PageNumber = page, PageSize = 100, Filter = new() { UserId = userId } }, cancellationToken);
            if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException("No se pudieron consultar las inscripciones del usuario.");
            var added = 0;
            foreach (var item in response.Data)
            {
                if (item.UserId != userId) throw new InvalidOperationException("La consulta devolvió inscripciones de otro usuario.");
                if (item.RegistrationId is not { } id || id == Guid.Empty) throw new InvalidOperationException("La API devolvió una inscripción sin identificador.");
                if (!seen.Add(id)) continue;
                added++;
                if (item.ClassId is { } candidate && candidate != Guid.Empty) candidates.TryAdd(candidate, item.ModuleName ?? "Curso del estudiante");
            }
            if (response.Data.Count < 100) break;
            if (added == 0) throw new InvalidOperationException("La consulta de inscripciones no avanzó de página.");
        }
        PointRegistrationTarget? target = null;
        foreach (var candidate in candidates)
        {
            // El resumen no siempre incluye Active; validarlo en el endpoint del curso.
            var id = await FindInCourseAsync(userId, candidate.Key, cancellationToken);
            if (id is null) continue;
            if (target is not null) throw new InvalidOperationException("El usuario tiene inscripciones activas en varios cursos. Asigna los puntos desde el curso correspondiente.");
            target = new(id.Value, candidate.Key, candidate.Value);
        }
        return target ?? throw new InvalidOperationException("No se encontraron inscripciones activas para este usuario.");
    }

    private async Task<Guid?> FindInCourseAsync(Guid userId, Guid classId, CancellationToken cancellationToken)
    {
        Guid? registrationId = null;
        var seen = new HashSet<Guid>();
        for (var page = 1; ; page++)
        {
            var response = await courses.GetRegistrationsAsync(classId, page, cancellationToken);
            if (response?.Data is null || response.Error is { ErrorCode: not 0 }) throw new InvalidOperationException("No se pudieron consultar las inscripciones del curso.");
            var added = 0;
            foreach (var item in response.Data)
            {
                if (item.ClassId != classId || item.Id == Guid.Empty || item.UserId == Guid.Empty) throw new InvalidOperationException("La API devolvió una inscripción no válida para este curso.");
                if (!seen.Add(item.Id)) continue;
                added++;
                if (!item.Active || item.UserId != userId) continue;
                if (registrationId.HasValue) throw new InvalidOperationException("El usuario tiene más de una inscripción activa en este curso.");
                registrationId = item.Id;
            }
            if (response.Data.Count < 100) break;
            if (added == 0) throw new InvalidOperationException("La consulta de inscripciones no avanzó de página.");
        }
        return registrationId;
    }
}
