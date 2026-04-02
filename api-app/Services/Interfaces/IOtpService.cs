namespace api_app.Services.Interfaces;

public interface IOtpService
{
    Task SendOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default);
    Task<bool> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}
