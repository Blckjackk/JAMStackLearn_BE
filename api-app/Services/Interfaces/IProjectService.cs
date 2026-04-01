using api_app.DTOs.Projects;

namespace api_app.Services.Interfaces;

public interface IProjectService
{
    Task<ProjectResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectResponseDto>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto, int userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, UpdateProjectDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(int projectId, CancellationToken cancellationToken = default);
    Task<ProjectMemberDto> AddMemberAsync(int projectId, int actorUserId, AddProjectMemberDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateMemberRoleAsync(int projectId, int targetUserId, int actorUserId, UpdateProjectMemberRoleDto dto, CancellationToken cancellationToken = default);
    Task<bool> RemoveMemberAsync(int projectId, int targetUserId, int actorUserId, CancellationToken cancellationToken = default);
    Task<ProjectInviteDto> CreateInviteAsync(int projectId, int actorUserId, CreateProjectInviteDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectInviteDto>> GetPendingInvitesAsync(int actorUserId, CancellationToken cancellationToken = default);
    Task<ProjectInviteDto> AcceptInviteAsync(int inviteId, int actorUserId, CancellationToken cancellationToken = default);
    Task<ProjectInviteDto> RejectInviteAsync(int inviteId, int actorUserId, CancellationToken cancellationToken = default);
}
