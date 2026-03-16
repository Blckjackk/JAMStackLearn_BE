using api_app.Models;

namespace api_app.Repositories.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Project> CreateAsync(Project project, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Project project, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
