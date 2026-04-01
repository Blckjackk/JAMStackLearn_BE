using api_app.DTOs.Users;

namespace api_app.Services.Interfaces;

public interface IFirebaseAuthService
{
    Task<UserResponseDto> LoginWithFirebaseAsync(string token, CancellationToken cancellationToken = default);
}
