namespace api_app.DTOs.Otp;

public class OtpSendResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}
