using api_app.Database;
using api_app.DTOs.Admin;
using api_app.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace api_app.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly DbConnection _connection;

    public AdminRepository(DbConnection connection)
    {
        _connection = connection;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT
                                 (SELECT COUNT(*) FROM Users) AS UsersCount,
                                 (SELECT COUNT(*) FROM Projects) AS ProjectsCount,
                                 (SELECT COUNT(*) FROM Tasks) AS TasksCount,
                                 (SELECT COUNT(*) FROM ProjectInvites) AS InvitesCount";

        await using var cmd = new SqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new AdminDashboardDto();
        }

        return new AdminDashboardDto
        {
            UsersCount = reader.GetInt32(reader.GetOrdinal("UsersCount")),
            ProjectsCount = reader.GetInt32(reader.GetOrdinal("ProjectsCount")),
            TasksCount = reader.GetInt32(reader.GetOrdinal("TasksCount")),
            InvitesCount = reader.GetInt32(reader.GetOrdinal("InvitesCount"))
        };
    }
}
