using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Projects;

public class UpdateProjectMemberRoleDto
{
    [Required]
    public string Role { get; set; } = "Editor"; // "Editor" atau "Viewer"
}
