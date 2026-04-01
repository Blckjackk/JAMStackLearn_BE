using api_app.DTOs.Users;
using api_app.Models;
using api_app.Repositories.Interfaces;
using api_app.Services.Interfaces;
using System.Net.Mail;

namespace api_app.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UserResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapUser).ToList();
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : MapUser(user);
    }

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            throw new ArgumentException("Username is required.");
        }

        if (!IsValidEmail(dto.Email))
        {
            throw new ArgumentException("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new ArgumentException("Password is required.");
        }

        var user = new User
        {
            Username = dto.Username.Trim(),
            Email = dto.Email.Trim(),
            Role = "Developer",
            PasswordHash = _passwordHasher.Hash(dto.Password)
        };

        var created = await _userRepository.CreateAsync(user, cancellationToken);
        return MapUser(created);
    }

    public async Task<UserResponseDto?> LoginAsync(LoginUserDto dto, CancellationToken cancellationToken = default)
    {
        if (!IsValidEmail(dto.Email))
        {
            throw new ArgumentException("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new ArgumentException("Password is required.");
        }

        var user = await _userRepository.GetByEmailAsync(dto.Email.Trim(), cancellationToken);
        if (user is null)
        {
            return null;
        }

        var isPasswordValid = _passwordHasher.Verify(dto.Password, user.PasswordHash);
        return isPasswordValid ? MapUser(user) : null;
    }

    public async Task<UserResponseDto?> UpdateProfileAsync(int userId, UpdateUserProfileDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            throw new ArgumentException("Username is required.");
        }

        var updated = await _userRepository.UpdateProfileAsync(userId, dto.Username.Trim(), cancellationToken);
        return updated is null ? null : MapUser(updated);
    }

    private static UserResponseDto MapUser(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            UserCode = user.UserCode,
            Role = user.Role
        };
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var parsed = new MailAddress(email.Trim());
            return string.Equals(parsed.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
