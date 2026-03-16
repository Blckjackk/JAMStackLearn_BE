namespace api_app.DTOs.Tasks;

public class TaskResponseDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public List<TagDto> Tags { get; set; } = [];
}
