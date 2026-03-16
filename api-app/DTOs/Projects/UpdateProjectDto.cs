using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Projects;

public class UpdateProjectDto
{
    [MaxLength(120)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
