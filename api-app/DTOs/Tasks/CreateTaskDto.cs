using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Tasks;

public class CreateTaskDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Content { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    [MaxLength(1, ErrorMessage = "Only one tag is allowed per task.")]
    public List<int> TagIds { get; set; } = [];
}
