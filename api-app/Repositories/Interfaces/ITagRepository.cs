using api_app.Models;

namespace api_app.Repositories.Interfaces;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default);
    Task SetTaskTagsAsync(int taskId, IReadOnlyCollection<int> tagIds, CancellationToken cancellationToken = default);
    Task<bool> ExistAllAsync(IReadOnlyCollection<int> tagIds, CancellationToken cancellationToken = default);
}
