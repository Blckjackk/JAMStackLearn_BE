using api_app.Models;

namespace api_app.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<TaskItem> CreateAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
