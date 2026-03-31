using api_app.DTOs.Tasks;

namespace api_app.Services.Interfaces;

public interface ITaskService
{
    Task<TaskResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskResponseDto>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, int actorUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, UpdateTaskDto dto, int actorUserId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
