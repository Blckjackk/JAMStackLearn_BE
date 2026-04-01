using api_app.Database;
using api_app.Models;
using api_app.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace api_app.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbConnection _connection;

    public UserRepository(DbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<User>();

        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = "SELECT Id, Username, Email, UserCode, PasswordHash FROM Users";

        await using var cmd = new SqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(MapUser(reader));
        }

        return users;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = "SELECT Id, Username, Email, UserCode, PasswordHash FROM Users WHERE Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapUser(reader);
        }

        return null;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = "SELECT TOP 1 Id, Username, Email, UserCode, PasswordHash FROM Users WHERE Email = @Email";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Email", email);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapUser(reader);
        }

        return null;
    }

    public async Task<User?> GetByIdentityAsync(string provider, string providerUserId, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT u.Id, u.Username, u.Email, u.UserCode, u.PasswordHash
                               FROM Users u
                               INNER JOIN UserIdentities ui ON ui.UserId = u.Id
                               WHERE ui.Provider = @Provider AND ui.ProviderUserId = @ProviderUserId";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Provider", provider);
        cmd.Parameters.AddWithValue("@ProviderUserId", providerUserId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapUser(reader);
        }

        return null;
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

           const string query = @"DECLARE @Inserted TABLE (
                                 Id INT,
                                 Username NVARCHAR(100),
                                 Email NVARCHAR(255),
                                 UserCode NVARCHAR(20),
                                 PasswordHash NVARCHAR(200)
                             );

                             INSERT INTO Users (Username, Email, UserCode, PasswordHash)
                             OUTPUT INSERTED.Id, INSERTED.Username, INSERTED.Email, INSERTED.UserCode, INSERTED.PasswordHash
                             INTO @Inserted
                             VALUES (@Username, @Email, @UserCode, @PasswordHash);

                             UPDATE Users
                             SET UserCode = CASE
                                 WHEN UserCode IS NULL OR UserCode = ''
                                 THEN 'USR-' + RIGHT('000000' + CAST(Id AS VARCHAR(6)), 6)
                                 ELSE UserCode
                             END
                             WHERE Id IN (SELECT Id FROM @Inserted);

                             SELECT u.Id, u.Username, u.Email, u.UserCode, u.PasswordHash
                             FROM Users u
                             INNER JOIN @Inserted i ON u.Id = i.Id;";

        await using var cmd = new SqlCommand(query, conn);

        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@UserCode", string.IsNullOrWhiteSpace(user.UserCode) ? string.Empty : user.UserCode);
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Failed to create user.");
        }

        return MapUser(reader);
    }

    public async Task<User?> UpdateProfileAsync(int userId, string username, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"UPDATE Users
                               SET Username = @Username
                               OUTPUT INSERTED.Id, INSERTED.Username, INSERTED.Email, INSERTED.UserCode, INSERTED.PasswordHash
                               WHERE Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", userId);
        cmd.Parameters.AddWithValue("@Username", username);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapUser(reader);
        }

        return null;
    }

    public async Task UpsertIdentityAsync(UserIdentity identity, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"IF EXISTS (SELECT 1 FROM UserIdentities WHERE Provider = @Provider AND ProviderUserId = @ProviderUserId)
BEGIN
    UPDATE UserIdentities
    SET UserId = @UserId,
        Email = @Email,
        DisplayName = @DisplayName,
        AvatarUrl = @AvatarUrl
    WHERE Provider = @Provider AND ProviderUserId = @ProviderUserId;
END
ELSE
BEGIN
    INSERT INTO UserIdentities (UserId, Provider, ProviderUserId, Email, DisplayName, AvatarUrl, CreatedAt)
    VALUES (@UserId, @Provider, @ProviderUserId, @Email, @DisplayName, @AvatarUrl, GETUTCDATE());
END";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@UserId", identity.UserId);
        cmd.Parameters.AddWithValue("@Provider", identity.Provider);
        cmd.Parameters.AddWithValue("@ProviderUserId", identity.ProviderUserId);
        cmd.Parameters.AddWithValue("@Email", (object?)identity.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DisplayName", (object?)identity.DisplayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AvatarUrl", (object?)identity.AvatarUrl ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = "SELECT 1 FROM Users WHERE Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        var exists = await cmd.ExecuteScalarAsync(cancellationToken);
        return exists is not null;
    }

    private static User MapUser(SqlDataReader reader)
    {
        return new User
        {
            Id = (int)reader["Id"],
            Username = Convert.ToString(reader["Username"]) ?? string.Empty,
            Email = Convert.ToString(reader["Email"]) ?? string.Empty,
            UserCode = reader["UserCode"] == DBNull.Value ? string.Empty : Convert.ToString(reader["UserCode"]) ?? string.Empty,
            PasswordHash = Convert.ToString(reader["PasswordHash"]) ?? string.Empty
        };
    }

    public async Task<User?> GetByUserCodeAsync(string userCode, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = "SELECT TOP 1 Id, Username, Email, UserCode, PasswordHash FROM Users WHERE UserCode = @UserCode";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@UserCode", userCode);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapUser(reader);
        }

        return null;
    }
}
