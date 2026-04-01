using api_app.Database;
using api_app.Models;
using api_app.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace api_app.Repositories;

public class ProjectInviteRepository : IProjectInviteRepository
{
    private readonly DbConnection _connection;

    public ProjectInviteRepository(DbConnection connection)
    {
        _connection = connection;
    }

    public async Task<ProjectInvite?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT pi.Id, pi.ProjectId, pi.InvitedUserId, pi.InvitedByUserId, pi.Role, pi.Status, pi.CreatedAt, pi.RespondedAt,
                                      p.Name AS ProjectName,
                                      u.Username AS InvitedUsername, u.UserCode AS InvitedUserCode,
                                      ib.Username AS InvitedByUsername
                               FROM ProjectInvites pi
                               INNER JOIN Projects p ON p.Id = pi.ProjectId
                               INNER JOIN Users u ON u.Id = pi.InvitedUserId
                               INNER JOIN Users ib ON ib.Id = pi.InvitedByUserId
                               WHERE pi.Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapInvite(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<ProjectInvite>> GetPendingByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var invites = new List<ProjectInvite>();

        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT pi.Id, pi.ProjectId, pi.InvitedUserId, pi.InvitedByUserId, pi.Role, pi.Status, pi.CreatedAt, pi.RespondedAt,
                                      p.Name AS ProjectName,
                                      u.Username AS InvitedUsername, u.UserCode AS InvitedUserCode,
                                      ib.Username AS InvitedByUsername
                               FROM ProjectInvites pi
                               INNER JOIN Projects p ON p.Id = pi.ProjectId
                               INNER JOIN Users u ON u.Id = pi.InvitedUserId
                               INNER JOIN Users ib ON ib.Id = pi.InvitedByUserId
                               WHERE pi.InvitedUserId = @UserId AND pi.Status = @Status
                               ORDER BY pi.CreatedAt DESC";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Status", InviteStatus.Pending);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            invites.Add(MapInvite(reader));
        }

        return invites;
    }

    public async Task<ProjectInvite?> GetPendingByProjectAndUserAsync(int projectId, int userId, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT TOP 1 pi.Id, pi.ProjectId, pi.InvitedUserId, pi.InvitedByUserId, pi.Role, pi.Status, pi.CreatedAt, pi.RespondedAt,
                                      p.Name AS ProjectName,
                                      u.Username AS InvitedUsername, u.UserCode AS InvitedUserCode,
                                      ib.Username AS InvitedByUsername
                               FROM ProjectInvites pi
                               INNER JOIN Projects p ON p.Id = pi.ProjectId
                               INNER JOIN Users u ON u.Id = pi.InvitedUserId
                               INNER JOIN Users ib ON ib.Id = pi.InvitedByUserId
                               WHERE pi.ProjectId = @ProjectId AND pi.InvitedUserId = @UserId AND pi.Status = @Status";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@ProjectId", projectId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Status", InviteStatus.Pending);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapInvite(reader);
        }

        return null;
    }

    public async Task<ProjectInvite> CreateAsync(ProjectInvite invite, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"DECLARE @Inserted TABLE (
                                    Id INT
                               );

                               INSERT INTO ProjectInvites (ProjectId, InvitedUserId, InvitedByUserId, Role, Status, CreatedAt)
                               OUTPUT INSERTED.Id INTO @Inserted
                               VALUES (@ProjectId, @InvitedUserId, @InvitedByUserId, @Role, @Status, @CreatedAt);

                               SELECT pi.Id, pi.ProjectId, pi.InvitedUserId, pi.InvitedByUserId, pi.Role, pi.Status, pi.CreatedAt, pi.RespondedAt,
                                      p.Name AS ProjectName,
                                      u.Username AS InvitedUsername, u.UserCode AS InvitedUserCode,
                                      ib.Username AS InvitedByUsername
                               FROM ProjectInvites pi
                               INNER JOIN @Inserted i ON pi.Id = i.Id
                               INNER JOIN Projects p ON p.Id = pi.ProjectId
                               INNER JOIN Users u ON u.Id = pi.InvitedUserId
                               INNER JOIN Users ib ON ib.Id = pi.InvitedByUserId;";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@ProjectId", invite.ProjectId);
        cmd.Parameters.AddWithValue("@InvitedUserId", invite.InvitedUserId);
        cmd.Parameters.AddWithValue("@InvitedByUserId", invite.InvitedByUserId);
        cmd.Parameters.AddWithValue("@Role", invite.Role);
        cmd.Parameters.AddWithValue("@Status", invite.Status);
        cmd.Parameters.AddWithValue("@CreatedAt", invite.CreatedAt);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Failed to create project invite.");
        }

        return MapInvite(reader);
    }

    public async Task<bool> UpdateStatusAsync(int inviteId, string status, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"UPDATE ProjectInvites
                               SET Status = @Status,
                                   RespondedAt = CASE
                                       WHEN @Status = @Pending THEN NULL
                                       ELSE GETUTCDATE()
                                   END
                               WHERE Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", inviteId);
        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@Pending", InviteStatus.Pending);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static ProjectInvite MapInvite(SqlDataReader reader)
    {
        return new ProjectInvite
        {
            Id = (int)reader["Id"],
            ProjectId = (int)reader["ProjectId"],
            InvitedUserId = (int)reader["InvitedUserId"],
            InvitedByUserId = (int)reader["InvitedByUserId"],
            Role = Convert.ToString(reader["Role"]) ?? TeamRoles.Viewer,
            Status = Convert.ToString(reader["Status"]) ?? InviteStatus.Pending,
            CreatedAt = reader["CreatedAt"] == DBNull.Value ? DateTime.UtcNow : (DateTime)reader["CreatedAt"],
            RespondedAt = reader["RespondedAt"] == DBNull.Value ? null : (DateTime)reader["RespondedAt"],
            Project = new Project
            {
                Id = (int)reader["ProjectId"],
                Name = Convert.ToString(reader["ProjectName"]) ?? string.Empty
            },
            InvitedUser = new User
            {
                Id = (int)reader["InvitedUserId"],
                Username = Convert.ToString(reader["InvitedUsername"]) ?? string.Empty,
                UserCode = Convert.ToString(reader["InvitedUserCode"]) ?? string.Empty
            },
            InvitedByUser = new User
            {
                Id = (int)reader["InvitedByUserId"],
                Username = Convert.ToString(reader["InvitedByUsername"]) ?? string.Empty
            }
        };
    }
}
