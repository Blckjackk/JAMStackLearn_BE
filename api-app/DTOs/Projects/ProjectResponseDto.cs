namespace api_app.DTOs.Projects;

public class ProjectResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ProjectMemberDto> Members { get; set; } = [];
    public string UserRole { get; set; } = "Viewer"; // Role dari user yang login
}
