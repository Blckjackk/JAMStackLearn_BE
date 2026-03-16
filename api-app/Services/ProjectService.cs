using api_app.DTOs.Projects;
using api_app.Models;
using api_app.Repositories.Interfaces;
using api_app.Services.Interfaces;

namespace api_app.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;

    public ProjectService(IProjectRepository projectRepository, IUserRepository userRepository)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
    }

    public async Task<ProjectResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
        return project is null ? null : MapProject(project);
    }

    public async Task<IReadOnlyList<ProjectResponseDto>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetByUserIdAsync(userId, cancellationToken);
        return projects.Select(MapProject).ToList();
    }

    public async Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Project name is required.");
        }

        var userExists = await _userRepository.ExistsAsync(dto.UserId, cancellationToken);
        if (!userExists)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var project = new Project
        {
            UserId = dto.UserId,
            Name = dto.Name.Trim(),
            Description = dto.Description ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _projectRepository.CreateAsync(project, cancellationToken);
        return MapProject(created);
    }

    public async Task<bool> UpdateAsync(int id, UpdateProjectDto dto, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return false;
        }

        if (dto.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Project name cannot be empty.");
            }

            project.Name = dto.Name.Trim();
        }

        if (dto.Description is not null)
        {
            project.Description = dto.Description;
        }

        return await _projectRepository.UpdateAsync(project, cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return _projectRepository.DeleteAsync(id, cancellationToken);
    }

    private static ProjectResponseDto MapProject(Project project)
    {
        return new ProjectResponseDto
        {
            Id = project.Id,
            UserId = project.UserId,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt
        };
    }
}
