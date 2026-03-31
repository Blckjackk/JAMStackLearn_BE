namespace api_app.DTOs.Tasks;

public class TaskResponseDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? AssigneeUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Status { get; set; } = "TODO";
    public string Priority { get; set; } = "Medium";
    public bool IsCompleted { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public List<TagDto> Tags { get; set; } = [];
}
