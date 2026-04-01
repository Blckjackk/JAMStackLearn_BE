using api_app.Models;

namespace api_app.Repositories.Interfaces;

public interface IProjectInviteRepository
{
    Task<ProjectInvite?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectInvite>> GetPendingByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<ProjectInvite> CreateAsync(ProjectInvite invite, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int inviteId, string status, CancellationToken cancellationToken = default);
    Task<ProjectInvite?> GetPendingByProjectAndUserAsync(int projectId, int userId, CancellationToken cancellationToken = default);
}
