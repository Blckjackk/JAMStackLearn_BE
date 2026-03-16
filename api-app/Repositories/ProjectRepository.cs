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

        const string query = @"SELECT Id, UserId, Name, Description, CreatedAt
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

        const string query = @"SELECT Id, UserId, Name, Description, CreatedAt
                               FROM Projects
                               WHERE UserId = @UserId";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);

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

        const string query = @"INSERT INTO Projects (UserId, Name, Description, CreatedAt)
                               OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.Name, INSERTED.Description, INSERTED.CreatedAt
                               VALUES (@UserId, @Name, @Description, @CreatedAt)";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@UserId", project.UserId);
        cmd.Parameters.AddWithValue("@Name", project.Name);
        cmd.Parameters.AddWithValue("@Description", project.Description);
        cmd.Parameters.AddWithValue("@CreatedAt", project.CreatedAt);

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
                                   Description = @Description
                               WHERE Id = @Id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", project.Id);
        cmd.Parameters.AddWithValue("@Name", project.Name);
        cmd.Parameters.AddWithValue("@Description", project.Description);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

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
            UserId = (int)reader["UserId"],
            Name = Convert.ToString(reader["Name"]) ?? string.Empty,
            Description = Convert.ToString(reader["Description"]) ?? string.Empty,
            CreatedAt = (DateTime)reader["CreatedAt"]
        };
    }
}
