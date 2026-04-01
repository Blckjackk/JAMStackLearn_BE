using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Projects;

public class CreateProjectInviteDto
{
    [Required]
    [MaxLength(20)]
    public string UserCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;
}
