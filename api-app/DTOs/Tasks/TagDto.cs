namespace api_app.DTOs.Tasks;

public class TagDto
{
    public int Id { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#000000";
}
