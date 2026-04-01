using api_app.DTOs.Users;

namespace api_app.Services.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserResponseDto> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default);
    Task<UserResponseDto?> LoginAsync(LoginUserDto dto, CancellationToken cancellationToken = default);
    Task<UserResponseDto?> UpdateProfileAsync(int userId, UpdateUserProfileDto dto, CancellationToken cancellationToken = default);
}
