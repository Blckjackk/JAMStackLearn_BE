using api_app.DTOs.Tasks;
using api_app.Models;
using api_app.Repositories.Interfaces;
using api_app.Services.Interfaces;

namespace api_app.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectUserRepository _projectUserRepository;
    private readonly ITagRepository _tagRepository;

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IProjectUserRepository projectUserRepository,
        ITagRepository tagRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _projectUserRepository = projectUserRepository;
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

    public async Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, int actorUserId, CancellationToken cancellationToken = default)
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

        var actorMembership = await _projectUserRepository.GetByProjectAndUserAsync(dto.ProjectId, actorUserId, cancellationToken);
        if (actorMembership is null)
        {
            throw new UnauthorizedAccessException("You are not a member of this project.");
        }

        if (!actorMembership.IsProjectManager)
        {
            throw new UnauthorizedAccessException("Only Project Manager can create tasks.");
        }

        if (dto.AssigneeUserId.HasValue)
        {
            if (!actorMembership.CanAssignTasks)
            {
                throw new UnauthorizedAccessException("Your role cannot assign tasks.");
            }

            var assigneeMembership = await _projectUserRepository.GetByProjectAndUserAsync(dto.ProjectId, dto.AssigneeUserId.Value, cancellationToken);
            if (assigneeMembership is null)
            {
                throw new ArgumentException("Assignee must be a member of this project.");
            }
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
            AssigneeUserId = dto.AssigneeUserId,
            Title = dto.Title.Trim(),
            Description = NormalizeOptionalText(dto.Description, 2000, "Description"),
            Content = NormalizeOptionalText(dto.Content, 4000, "Content"),
            Status = NormalizeStatus(dto.Status),
            Priority = NormalizePriority(dto.Priority),
            IsCompleted = false,
            DueDate = dto.DueDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _taskRepository.CreateAsync(task, cancellationToken);

        if (dto.TagIds.Count > 0)
        {
            await _tagRepository.SetTaskTagsAsync(created.Id, dto.TagIds, cancellationToken);
        }

        var tags = await _tagRepository.GetByTaskIdAsync(created.Id, cancellationToken);
        return MapTask(created, tags);
    }

    public async Task<bool> UpdateAsync(int id, UpdateTaskDto dto, int actorUserId, CancellationToken cancellationToken = default)
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

        var actorMembership = await _projectUserRepository.GetByProjectAndUserAsync(existingTask.ProjectId, actorUserId, cancellationToken);
        if (actorMembership is null)
        {
            throw new UnauthorizedAccessException("You are not a member of this project.");
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
            existingTask.Content = NormalizeOptionalText(dto.Content, 4000, "Content");
        }

        if (dto.Description is not null)
        {
            existingTask.Description = NormalizeOptionalText(dto.Description, 2000, "Description");
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

        if (dto.Status is not null)
        {
            existingTask.Status = NormalizeRequiredValue(dto.Status, 50, "Status");
        }

        if (dto.Priority is not null)
        {
            existingTask.Priority = NormalizeRequiredValue(dto.Priority, 50, "Priority");
        }

        if (dto.AssigneeUserId.HasValue)
        {
            if (!actorMembership.CanAssignTasks)
            {
                throw new UnauthorizedAccessException("Your role cannot assign tasks.");
            }

            var assigneeMembership = await _projectUserRepository.GetByProjectAndUserAsync(existingTask.ProjectId, dto.AssigneeUserId.Value, cancellationToken);
            if (assigneeMembership is null)
            {
                throw new ArgumentException("Assignee must be a member of this project.");
            }

            existingTask.AssigneeUserId = dto.AssigneeUserId;
        }

        existingTask.UpdatedAt = DateTime.UtcNow;
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
            AssigneeUserId = task.AssigneeUserId,
            Title = task.Title,
            Description = task.Description,
            Content = task.Content,
            Status = task.Status,
            Priority = task.Priority,
            IsCompleted = task.IsCompleted,
            DueDate = task.DueDate,
            Tags = tags.Select(tag => new TagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Color = tag.Color
            }).ToList()
        };
    }

    private static string NormalizeOptionalText(string? value, int maxLength, string fieldName)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{fieldName} max length is {maxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeRequiredValue(string? value, int maxLength, string fieldName)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException($"{fieldName} is required.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{fieldName} max length is {maxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "TODO"
            : NormalizeRequiredValue(status, 50, "Status");
    }

    private static string NormalizePriority(string? priority)
    {
        return string.IsNullOrWhiteSpace(priority)
            ? "Medium"
            : NormalizeRequiredValue(priority, 50, "Priority");
    }
}
