namespace api_app.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<ProjectUser> ProjectMemberships { get; set; } = [];
        public List<TaskItem> AssignedTasks { get; set; } = [];
        public List<UserIdentity> Identities { get; set; } = [];

        public string GetDisplayName()
        {
            return $"{Username} ({Email})";
        }
    }
}
