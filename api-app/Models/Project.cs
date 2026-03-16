namespace api_app.Models
{
    public class Project
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public bool BelongsToUser(int userId)
        {
            return UserId == userId;
        }

        public int GetProjectAgeDays()
        {
            return (DateTime.UtcNow - CreatedAt).Days;
        }

        public string GetSummary()
        {
            return $"{Name} - Created {CreatedAt.ToShortDateString()}";
        }
    }
}
