using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Users;

public class FirebaseLoginRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
