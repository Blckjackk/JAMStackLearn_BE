using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Projects;

public class CreateProjectDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}
