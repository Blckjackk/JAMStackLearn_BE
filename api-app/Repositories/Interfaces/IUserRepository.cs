using api_app.Models;

namespace api_app.Repositories.Interfaces;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUserCodeAsync(string userCode, CancellationToken cancellationToken = default);
    Task<User?> GetByIdentityAsync(string provider, string providerUserId, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> UpdateProfileAsync(int userId, string username, CancellationToken cancellationToken = default);
    Task<User?> UpdateOtpVerificationAsync(int userId, string phoneNumber, bool isOtpVerified, CancellationToken cancellationToken = default);
    Task UpsertIdentityAsync(UserIdentity identity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
