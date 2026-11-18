using GrammarErrorDetection.Models.Classrooms;
using GrammarErrorDetection.Models.Responses;

namespace GrammarErrorDetection.Services.Interfaces;

/// <summary>Gestión de aulas (<c>api/Classroom</c>); el alta y la edición son solo del coordinador.</summary>
public interface IClassroomCatalogService
{
    /// <summary>Página de aulas; <see cref="ApiResponse{T}.TotalCount"/> es el total sin paginar.</summary>
    Task<ApiResponse<List<ClassroomItem>>?> SearchAsync(ClassroomSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ClassroomItem>?> CreateAsync(ClassroomCreateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ClassroomItem>?> UpdateAsync(ClassroomUpdateRequest request, CancellationToken cancellationToken = default);
}
