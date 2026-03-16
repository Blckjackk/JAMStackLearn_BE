using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Tasks;

public class UpdateTaskDto
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(4000)]
    public string? Content { get; set; }

    public bool? IsCompleted { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public List<int>? TagIds { get; set; }
}
