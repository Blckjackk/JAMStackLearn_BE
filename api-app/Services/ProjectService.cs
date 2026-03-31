using api_app.DTOs.Projects;
using api_app.Models;
using api_app.Repositories.Interfaces;
using api_app.Services.Interfaces;

namespace api_app.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectUserRepository _projectUserRepository;
    private readonly IUserRepository _userRepository;

    public ProjectService(
        IProjectRepository projectRepository,
        IProjectUserRepository projectUserRepository,
        IUserRepository userRepository)
    {
        _projectRepository = projectRepository;
        _projectUserRepository = projectUserRepository;
        _userRepository = userRepository;
    }

    public async Task<ProjectResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var members = await _projectUserRepository.GetByProjectIdAsync(id, cancellationToken);
        return MapProject(project, members);
    }

    public async Task<IReadOnlyList<ProjectResponseDto>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetByUserIdAsync(userId, cancellationToken);
        var result = new List<ProjectResponseDto>(projects.Count);

        foreach (var project in projects)
        {
            var members = await _projectUserRepository.GetByProjectIdAsync(project.Id, cancellationToken);
            result.Add(MapProject(project, members, userId));
        }

        return result;
    }

    public async Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto, int userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Project name is required.");
        }

        var userExists = await _userRepository.ExistsAsync(userId, cancellationToken);
        if (!userExists)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var project = new Project
        {
            Name = dto.Name.Trim(),
            Description = dto.Description ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _projectRepository.CreateAsync(project, cancellationToken);

        await _projectUserRepository.CreateAsync(new ProjectUser
        {
            ProjectId = created.Id,
            UserId = userId,
            Role = TeamRoles.ProjectManager,
            JoinedAt = DateTime.UtcNow
        }, cancellationToken);

        var members = await _projectUserRepository.GetByProjectIdAsync(created.Id, cancellationToken);
        return MapProject(created, members, userId);
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

    public async Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var members = await _projectUserRepository.GetByProjectIdAsync(projectId, cancellationToken);
        return members.Select(MapMember).ToList();
    }

    public async Task<ProjectMemberDto> AddMemberAsync(
        int projectId,
        int actorUserId,
        AddProjectMemberDto dto,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireProjectMembershipAsync(projectId, actorUserId, cancellationToken);
        if (!actor.CanManageMembers)
        {
            throw new UnauthorizedAccessException("Only Project Manager can add members.");
        }

        var userExists = await _userRepository.ExistsAsync(dto.UserId, cancellationToken);
        if (!userExists)
        {
            throw new KeyNotFoundException("Target user not found.");
        }

        var existing = await _projectUserRepository.GetByProjectAndUserAsync(projectId, dto.UserId, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("User is already a member of this project.");
        }

        var role = NormalizeRole(dto.Role);
        var created = await _projectUserRepository.CreateAsync(new ProjectUser
        {
            ProjectId = projectId,
            UserId = dto.UserId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        }, cancellationToken);

        return MapMember(created);
    }

    public async Task<bool> UpdateMemberRoleAsync(
        int projectId,
        int targetUserId,
        int actorUserId,
        UpdateProjectMemberRoleDto dto,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireProjectMembershipAsync(projectId, actorUserId, cancellationToken);
        if (!actor.CanManageMembers)
        {
            throw new UnauthorizedAccessException("Only Project Manager can update member roles.");
        }

        var target = await _projectUserRepository.GetByProjectAndUserAsync(projectId, targetUserId, cancellationToken);
        if (target is null)
        {
            return false;
        }

        var nextRole = NormalizeRole(dto.Role);
        if (target.UserId == actorUserId && !string.Equals(nextRole, TeamRoles.ProjectManager, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Project Manager cannot demote themselves.");
        }

        target.Role = nextRole;
        return await _projectUserRepository.UpdateAsync(target, cancellationToken);
    }

    public async Task<bool> RemoveMemberAsync(
        int projectId,
        int targetUserId,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireProjectMembershipAsync(projectId, actorUserId, cancellationToken);
        if (!actor.CanManageMembers)
        {
            throw new UnauthorizedAccessException("Only Project Manager can remove members.");
        }

        if (targetUserId == actorUserId)
        {
            throw new InvalidOperationException("Project Manager cannot remove themselves.");
        }

        return await _projectUserRepository.DeleteByProjectAndUserAsync(projectId, targetUserId, cancellationToken);
    }

    private async Task<ProjectUser> RequireProjectMembershipAsync(int projectId, int userId, CancellationToken cancellationToken)
    {
        var membership = await _projectUserRepository.GetByProjectAndUserAsync(projectId, userId, cancellationToken);
        if (membership is null)
        {
            throw new UnauthorizedAccessException("You are not a member of this project.");
        }

        return membership;
    }

    private static string NormalizeRole(string inputRole)
    {
        var role = inputRole?.Trim();
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role is required.");
        }

        if (role.Length > 50)
        {
            throw new ArgumentException("Role max length is 50 characters.");
        }

        return role;
    }

    private static ProjectResponseDto MapProject(Project project, IReadOnlyList<ProjectUser> members, int? actorUserId = null)
    {
        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            Members = members.Select(MapMember).ToList(),
            UserRole = actorUserId.HasValue
                ? (members.FirstOrDefault(m => m.UserId == actorUserId.Value)?.Role ?? TeamRoles.Viewer)
                : TeamRoles.Viewer
        };
    }

    private static ProjectMemberDto MapMember(ProjectUser member)
    {
        return new ProjectMemberDto
        {
            UserId = member.UserId,
            Username = member.User?.Username ?? "Unknown",
            Email = member.User?.Email ?? "Unknown",
            Role = member.Role,
            JoinedAt = member.JoinedAt
        };
    }
}
