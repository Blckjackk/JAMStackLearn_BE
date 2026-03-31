namespace api_app.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public List<ProjectUser> Members { get; set; } = [];
        public List<TaskItem> Tasks { get; set; } = [];

        public int GetProjectAgeDays()
        {
            return (DateTime.UtcNow - CreatedAt).Days;
        }

        public string GetSummary()
        {
            return $"{Name} - Created {CreatedAt.ToShortDateString()}";
        }

        public bool HasMember(int userId)
        {
            return Members.Any(m => m.UserId == userId);
        }

        public ProjectUser? GetMember(int userId)
        {
            return Members.FirstOrDefault(m => m.UserId == userId);
        }

        public ProjectUser? GetOwner()
        {
            return Members.FirstOrDefault(m => m.IsProjectManager);
        }
    }
}
