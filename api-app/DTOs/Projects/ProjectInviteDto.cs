namespace api_app.DTOs.Projects;

public class ProjectInviteDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int InvitedUserId { get; set; }
    public string InvitedUserCode { get; set; } = string.Empty;
    public int InvitedByUserId { get; set; }
    public string InvitedByUsername { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
