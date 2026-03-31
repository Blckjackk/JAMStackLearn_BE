using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Projects;

public class AddProjectMemberDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string Role { get; set; } = "Editor"; // "Editor" atau "Viewer"
}
