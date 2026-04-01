namespace api_app.Services.Interfaces;

public interface IOtpService
{
    Task SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<bool> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}
