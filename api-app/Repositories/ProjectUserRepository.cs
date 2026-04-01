using api_app.Database;
using api_app.Models;
using api_app.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace api_app.Repositories;

public class ProjectUserRepository : IProjectUserRepository
{
    private readonly DbConnection _connection;

    public ProjectUserRepository(DbConnection connection)
    {
        _connection = connection;
    }

    public async Task<ProjectUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT pu.Id, pu.ProjectId, pu.UserId, pu.Role, pu.JoinedAt,
                  u.Username, u.Email, u.UserCode
                       FROM ProjectUsers pu
                       LEFT JOIN Users u ON pu.UserId = u.Id
                               WHERE pu.Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapProjectUser(reader);
        }

        return null;
    }

    public async Task<ProjectUser?> GetByProjectAndUserAsync(int projectId, int userId, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT pu.Id, pu.ProjectId, pu.UserId, pu.Role, pu.JoinedAt,
                  u.Username, u.Email, u.UserCode
                       FROM ProjectUsers pu
                       LEFT JOIN Users u ON pu.UserId = u.Id
                               WHERE pu.ProjectId = @ProjectId AND pu.UserId = @UserId";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@ProjectId", projectId);
        cmd.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapProjectUser(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<ProjectUser>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var members = new List<ProjectUser>();

        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT pu.Id, pu.ProjectId, pu.UserId, pu.Role, pu.JoinedAt,
                  u.Username, u.Email, u.UserCode
                       FROM ProjectUsers pu
                       LEFT JOIN Users u ON pu.UserId = u.Id
                               WHERE pu.ProjectId = @ProjectId
                               ORDER BY pu.Role DESC, pu.JoinedAt ASC";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@ProjectId", projectId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            members.Add(MapProjectUser(reader));
        }

        return members;
    }

    public async Task<IReadOnlyList<ProjectUser>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var projects = new List<ProjectUser>();

        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT pu.Id, pu.ProjectId, pu.UserId, pu.Role, pu.JoinedAt,
                  u.Username, u.Email, u.UserCode
                       FROM ProjectUsers pu
                       LEFT JOIN Users u ON pu.UserId = u.Id
                               WHERE pu.UserId = @UserId
                               ORDER BY pu.JoinedAt DESC";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            projects.Add(MapProjectUser(reader));
        }

        return projects;
    }

    public async Task<ProjectUser> CreateAsync(ProjectUser projectUser, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

         const string query = @"DECLARE @Inserted TABLE (
                        Id INT,
                        ProjectId INT,
                        UserId INT,
                        Role NVARCHAR(100),
                        JoinedAt DATETIME2
                       );

                       INSERT INTO ProjectUsers (ProjectId, UserId, Role, JoinedAt)
                       OUTPUT INSERTED.Id, INSERTED.ProjectId, INSERTED.UserId, INSERTED.Role, INSERTED.JoinedAt
                       INTO @Inserted
                       VALUES (@ProjectId, @UserId, @Role, @JoinedAt);

                       SELECT i.Id, i.ProjectId, i.UserId, i.Role, i.JoinedAt,
                           u.Username, u.Email, u.UserCode
                       FROM @Inserted i
                       LEFT JOIN Users u ON i.UserId = u.Id;";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@ProjectId", projectUser.ProjectId);
        cmd.Parameters.AddWithValue("@UserId", projectUser.UserId);
        cmd.Parameters.AddWithValue("@Role", projectUser.Role);
        cmd.Parameters.AddWithValue("@JoinedAt", projectUser.JoinedAt);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Failed to add project member.");
        }

        return MapProjectUser(reader);
    }

    public async Task<bool> UpdateAsync(ProjectUser projectUser, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"UPDATE ProjectUsers
                               SET Role = @Role
                               WHERE Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", projectUser.Id);
        cmd.Parameters.AddWithValue("@Role", projectUser.Role);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = "DELETE FROM ProjectUsers WHERE Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteByProjectAndUserAsync(int projectId, int userId, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = "DELETE FROM ProjectUsers WHERE ProjectId = @ProjectId AND UserId = @UserId";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@ProjectId", projectId);
        cmd.Parameters.AddWithValue("@UserId", userId);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static ProjectUser MapProjectUser(SqlDataReader reader)
    {
        return new ProjectUser
        {
            Id = (int)reader["Id"],
            ProjectId = (int)reader["ProjectId"],
            UserId = (int)reader["UserId"],
            Role = Convert.ToString(reader["Role"]) ?? TeamRoles.Frontend,
            JoinedAt = (DateTime)reader["JoinedAt"],
            User = new User
            {
                Id = (int)reader["UserId"],
                Username = reader["Username"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Username"]) ?? string.Empty,
                Email = reader["Email"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Email"]) ?? string.Empty,
                UserCode = reader["UserCode"] == DBNull.Value ? string.Empty : Convert.ToString(reader["UserCode"]) ?? string.Empty
            }
        };
    }
}
