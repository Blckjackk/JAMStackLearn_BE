namespace api_app.DTOs.Projects;

public class ProjectMemberDto
{
    public int UserId { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Owner", "Editor", "Viewer"
    public DateTime JoinedAt { get; set; }
}
