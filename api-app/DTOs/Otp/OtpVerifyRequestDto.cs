using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Otp;

public class OtpVerifyRequestDto
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    public int? UserId { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(6)]
    public string Code { get; set; } = string.Empty;
}
