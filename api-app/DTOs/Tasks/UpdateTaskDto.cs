using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Tasks;

public class UpdateTaskDto
{
    public int? AssigneeUserId { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(4000)]
    public string? Content { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(50)]
    public string? Priority { get; set; }

    public bool? IsCompleted { get; set; }
    public DateTimeOffset? DueDate { get; set; }

    [MaxLength(1, ErrorMessage = "Only one tag is allowed per task.")]
    public List<int>? TagIds { get; set; }
}
