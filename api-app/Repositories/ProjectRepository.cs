using api_app.Database;
using api_app.Models;
using api_app.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace api_app.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly DbConnection _connection;

    public ProjectRepository(DbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT Id, Name, Description, CreatedAt, UpdatedAt
                               FROM Projects
                               WHERE Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapProject(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<Project>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var projects = new List<Project>();

        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT p.Id, p.Name, p.Description, p.CreatedAt, p.UpdatedAt
                               FROM Projects p
                               INNER JOIN ProjectUsers pu ON p.Id = pu.ProjectId
                               WHERE pu.UserId = @UserId
                               ORDER BY p.CreatedAt DESC";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            projects.Add(MapProject(reader));
        }

        return projects;
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var projects = new List<Project>();

        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT Id, Name, Description, CreatedAt, UpdatedAt
                               FROM Projects
                               ORDER BY CreatedAt DESC";

        await using var cmd = new SqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            projects.Add(MapProject(reader));
        }

        return projects;
    }

    public async Task<Project> CreateAsync(Project project, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"INSERT INTO Projects (Name, Description, CreatedAt, UpdatedAt)
                               OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.Description, INSERTED.CreatedAt, INSERTED.UpdatedAt
                               VALUES (@Name, @Description, @CreatedAt, @UpdatedAt)";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Name", project.Name);
        cmd.Parameters.AddWithValue("@Description", project.Description);
        cmd.Parameters.AddWithValue("@CreatedAt", project.CreatedAt);
        cmd.Parameters.AddWithValue("@UpdatedAt", project.UpdatedAt);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Failed to create project.");
        }

        return MapProject(reader);
    }

    public async Task<bool> UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"UPDATE Projects
                               SET Name = @Name,
                                   Description = @Description,
                                   UpdatedAt = @UpdatedAt
                               WHERE Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", project.Id);
        cmd.Parameters.AddWithValue("@Name", project.Name);
        cmd.Parameters.AddWithValue("@Description", project.Description);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        // Delete related ProjectUsers first to maintain referential integrity
        const string deleteProjectUsersQuery = "DELETE FROM ProjectUsers WHERE ProjectId = @Id";
        await using var deleteUsersCmd = new SqlCommand(deleteProjectUsersQuery, conn);
        deleteUsersCmd.Parameters.AddWithValue("@Id", id);
        await deleteUsersCmd.ExecuteNonQueryAsync(cancellationToken);

        // Delete related Tasks
        const string deleteTasksQuery = "DELETE FROM Tasks WHERE ProjectId = @Id";
        await using var deleteTasksCmd = new SqlCommand(deleteTasksQuery, conn);
        deleteTasksCmd.Parameters.AddWithValue("@Id", id);
        await deleteTasksCmd.ExecuteNonQueryAsync(cancellationToken);

        // Delete project
        const string query = "DELETE FROM Projects WHERE Id = @Id";
        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = "SELECT 1 FROM Projects WHERE Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        var exists = await cmd.ExecuteScalarAsync(cancellationToken);
        return exists is not null;
    }

    private static Project MapProject(SqlDataReader reader)
    {
        return new Project
        {
            Id = (int)reader["Id"],
            Name = Convert.ToString(reader["Name"]) ?? string.Empty,
            Description = Convert.ToString(reader["Description"]) ?? string.Empty,
            CreatedAt = (DateTime)reader["CreatedAt"],
            UpdatedAt = (DateTime)reader["UpdatedAt"]
        };
    }
}
