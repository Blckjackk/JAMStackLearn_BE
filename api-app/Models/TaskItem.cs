namespace api_app.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? AssigneeUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = "TODO";
        public string Priority { get; set; } = "Medium";
        public bool IsCompleted { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Project? Project { get; set; }
        public User? AssigneeUser { get; set; }
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
