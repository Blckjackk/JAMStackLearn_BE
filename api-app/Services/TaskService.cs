using api_app.DTOs.Tasks;
using api_app.Models;
using api_app.Repositories.Interfaces;
using api_app.Services.Interfaces;

namespace api_app.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITagRepository _tagRepository;

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        ITagRepository tagRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _tagRepository = tagRepository;
    }

    public async Task<TaskResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var tags = await _tagRepository.GetByTaskIdAsync(task.Id, cancellationToken);
        return MapTask(task, tags);
    }

    public async Task<IReadOnlyList<TaskResponseDto>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var result = new List<TaskResponseDto>(tasks.Count);

        foreach (var task in tasks)
        {
            var tags = await _tagRepository.GetByTaskIdAsync(task.Id, cancellationToken);
            result.Add(MapTask(task, tags));
        }

        return result;
    }

    public async Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default)
    {
        ValidateSingleTagSelection(dto.TagIds);

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Task title is required.");
        }

        if (dto.DueDate.HasValue && dto.DueDate.Value.UtcDateTime < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Due date cannot be in the past.");
        }

        var projectExists = await _projectRepository.ExistsAsync(dto.ProjectId, cancellationToken);
        if (!projectExists)
        {
            throw new KeyNotFoundException("Project not found.");
        }

        if (dto.TagIds.Count > 0)
        {
            var allTagsExist = await _tagRepository.ExistAllAsync(dto.TagIds, cancellationToken);
            if (!allTagsExist)
            {
                throw new ArgumentException("One or more tags do not exist.");
            }
        }

        var task = new TaskItem
        {
            ProjectId = dto.ProjectId,
            Title = dto.Title.Trim(),
            Content = dto.Content,
            IsCompleted = false,
            DueDate = dto.DueDate
        };

        var created = await _taskRepository.CreateAsync(task, cancellationToken);

        if (dto.TagIds.Count > 0)
        {
            await _tagRepository.SetTaskTagsAsync(created.Id, dto.TagIds, cancellationToken);
        }

        var tags = await _tagRepository.GetByTaskIdAsync(created.Id, cancellationToken);
        return MapTask(created, tags);
    }

    public async Task<bool> UpdateAsync(int id, UpdateTaskDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.TagIds is not null)
        {
            ValidateSingleTagSelection(dto.TagIds);
        }

        var existingTask = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (existingTask is null)
        {
            return false;
        }

        if (dto.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                throw new ArgumentException("Task title cannot be empty.");
            }

            existingTask.Title = dto.Title.Trim();
        }

        if (dto.Content is not null)
        {
            existingTask.Content = dto.Content;
        }

        if (dto.IsCompleted.HasValue)
        {
            existingTask.IsCompleted = dto.IsCompleted.Value;
        }

        if (dto.DueDate.HasValue && dto.DueDate.Value.UtcDateTime < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Due date cannot be in the past.");
        }

        if (dto.DueDate is not null)
        {
            existingTask.DueDate = dto.DueDate;
        }

        var updated = await _taskRepository.UpdateAsync(existingTask, cancellationToken);

        if (updated && dto.TagIds is not null)
        {
            var allTagsExist = await _tagRepository.ExistAllAsync(dto.TagIds, cancellationToken);
            if (!allTagsExist)
            {
                throw new ArgumentException("One or more tags do not exist.");
            }

            await _tagRepository.SetTaskTagsAsync(id, dto.TagIds, cancellationToken);
        }

        return updated;
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return _taskRepository.DeleteAsync(id, cancellationToken);
    }

    private static void ValidateSingleTagSelection(IReadOnlyCollection<int> tagIds)
    {
        if (tagIds.Count > 1)
        {
            throw new ArgumentException("Only one tag is allowed per task.");
        }
    }

    private static TaskResponseDto MapTask(TaskItem task, IReadOnlyList<Tag> tags)
    {
        return new TaskResponseDto
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Title = task.Title,
            Content = task.Content,
            IsCompleted = task.IsCompleted,
            DueDate = task.DueDate,
            Tags = tags.Select(tag => new TagDto
            {
                Id = tag.Id,
                TagName = tag.TagName,
                ColorHex = tag.ColorHex
            }).ToList()
        };
    }
}
