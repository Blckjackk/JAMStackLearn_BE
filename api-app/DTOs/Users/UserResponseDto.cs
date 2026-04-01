namespace api_app.DTOs.Users;

public class UserResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
