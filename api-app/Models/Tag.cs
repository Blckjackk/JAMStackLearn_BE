namespace api_app.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#3b82f6";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<TaskItem> Tasks { get; set; } = [];

        public bool IsUrgent()
        {
            return string.Equals(Name, "urgent", StringComparison.OrdinalIgnoreCase);
        }

        public string GetDisplayTag()
        {
            return $"#{Name}";
        }
    }
}
