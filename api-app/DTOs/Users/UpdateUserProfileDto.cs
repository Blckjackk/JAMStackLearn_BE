using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Users;

public class UpdateUserProfileDto
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
}
