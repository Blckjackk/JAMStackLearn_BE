namespace api_app.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public bool IsCompleted { get; set; }
        public DateTimeOffset? DueDate { get; set; }

        public List<Tag> Tags { get; set; } = [];

        public void MarkCompleted()
        {
            IsCompleted = true;
        }

        public void MarkIncomplete()
        {
            IsCompleted = false;
        }

        public bool IsOverdue()
        {
            return DueDate.HasValue && DueDate.Value.UtcDateTime < DateTime.UtcNow && !IsCompleted;
        }

        public bool HasDueDate()
        {
            return DueDate.HasValue;
        }
    }
}
