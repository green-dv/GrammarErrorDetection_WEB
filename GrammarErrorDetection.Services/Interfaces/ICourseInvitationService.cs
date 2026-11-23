using GrammarErrorDetection.Models.Registrations;
using GrammarErrorDetection.Models.Responses;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ICourseInvitationService
{
    /// <summary>Genera el token del enlace/QR de invitación de un curso (docente del curso o coordinador).</summary>
    Task<ApiResponse<CourseInvitationLink>?> CreateAsync(Guid classId, CancellationToken cancellationToken = default);

    /// <summary>Valida la invitación y devuelve los datos del curso sin inscribir al estudiante.</summary>
    Task<ApiResponse<CourseJoinResult>?> PreviewAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Inscribe al estudiante autenticado en el curso de la invitación.</summary>
    Task<ApiResponse<CourseJoinResult>?> JoinAsync(string token, CancellationToken cancellationToken = default);
}
