using System.ComponentModel.DataAnnotations;

namespace api_app.DTOs.Otp;

public class OtpSendRequestDto
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
}
