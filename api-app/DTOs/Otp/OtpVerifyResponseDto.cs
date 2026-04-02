using api_app.DTOs.Users;

namespace api_app.DTOs.Otp;

public class OtpVerifyResponseDto
{
    public string Message { get; set; } = string.Empty;
    public UserResponseDto? User { get; set; }
}
