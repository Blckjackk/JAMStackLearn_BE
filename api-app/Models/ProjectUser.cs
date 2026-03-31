namespace api_app.Models
{
    public static class TeamRoles
    {
        public const string ProjectManager = "Project Manager";
        public const string Frontend = "Frontend";
        public const string Backend = "Backend";
        public const string QA = "QA";
        public const string DevOps = "DevOps";
        public const string Viewer = "Viewer";

        public static readonly HashSet<string> AssignmentRoles =
        [
            ProjectManager,
            Backend,
            Frontend,
            DevOps
        ];
    }

    public class ProjectUser
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = TeamRoles.Frontend;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Project? Project { get; set; }
        public User? User { get; set; }

        public bool IsProjectManager =>
            string.Equals(Role, TeamRoles.ProjectManager, StringComparison.OrdinalIgnoreCase);

        public bool CanEdit => !string.Equals(Role, TeamRoles.Viewer, StringComparison.OrdinalIgnoreCase);

        public bool CanManageMembers => IsProjectManager;

        public bool CanAssignTasks =>
            TeamRoles.AssignmentRoles.Contains(Role, StringComparer.OrdinalIgnoreCase);
    }
}
