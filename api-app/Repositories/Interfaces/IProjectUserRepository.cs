using api_app.Models;

namespace api_app.Repositories.Interfaces;

public interface IProjectUserRepository
{
    Task<ProjectUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProjectUser?> GetByProjectAndUserAsync(int projectId, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectUser>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectUser>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<ProjectUser> CreateAsync(ProjectUser projectUser, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ProjectUser projectUser, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteByProjectAndUserAsync(int projectId, int userId, CancellationToken cancellationToken = default);
}
