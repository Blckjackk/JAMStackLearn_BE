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

        const string query = "SELECT Id, Username, Email, PasswordHash FROM Users";

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

        const string query = "SELECT Id, Username, Email, PasswordHash FROM Users WHERE Id = @Id";

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

        const string query = "SELECT TOP 1 Id, Username, Email, PasswordHash FROM Users WHERE Email = @Email";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Email", email);

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

        const string query = @"INSERT INTO Users (Username, Email, PasswordHash)
                               OUTPUT INSERTED.Id, INSERTED.Username, INSERTED.Email, INSERTED.PasswordHash
                               VALUES (@Username, @Email, @PasswordHash)";

        await using var cmd = new SqlCommand(query, conn);

        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Failed to create user.");
        }

        return MapUser(reader);
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
            PasswordHash = Convert.ToString(reader["PasswordHash"]) ?? string.Empty
        };
    }
}
