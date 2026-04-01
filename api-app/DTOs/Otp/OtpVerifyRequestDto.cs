using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Otp;

public class OtpVerifyRequestDto
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(6)]
    public string Code { get; set; } = string.Empty;
}
