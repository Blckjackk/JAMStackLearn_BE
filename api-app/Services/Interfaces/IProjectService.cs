using api_app.DTOs.Projects;

namespace api_app.Services.Interfaces;

public interface IProjectService
{
    Task<ProjectResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectResponseDto>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, UpdateProjectDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
