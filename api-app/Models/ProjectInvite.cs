namespace api_app.Models
{
    public class ProjectInvite
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int InvitedUserId { get; set; }
        public int InvitedByUserId { get; set; }
        public string Role { get; set; } = TeamRoles.Viewer;
        public string Status { get; set; } = InviteStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }

        public Project? Project { get; set; }
        public User? InvitedUser { get; set; }
        public User? InvitedByUser { get; set; }
    }

    public static class InviteStatus
    {
        public const string Pending = "Pending";
        public const string Accepted = "Accepted";
        public const string Rejected = "Rejected";
    }
}
