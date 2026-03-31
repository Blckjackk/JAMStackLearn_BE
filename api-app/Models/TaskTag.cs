namespace api_app.Models
{
    public class TaskTag
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int TagId { get; set; }

        // Navigation
        public TaskItem? Task { get; set; }
        public Tag? Tag { get; set; }
    }
}
