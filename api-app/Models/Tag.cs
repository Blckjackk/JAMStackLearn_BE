namespace api_app.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#3b82f6";

        public bool IsUrgent()
        {
            return string.Equals(TagName, "urgent", StringComparison.OrdinalIgnoreCase);
        }

        public string GetDisplayTag()
        {
            return $"#{TagName}";
        }
    }
}
