using api_app.Database;
using api_app.Models;
using api_app.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace api_app.Repositories;

public class TagRepository : ITagRepository
{
    private readonly DbConnection _connection;

    public TagRepository(DbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tags = new List<Tag>();

        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT Id, Name, Color
                               FROM Tags
                       ORDER BY Name";

        await using var cmd = new SqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            tags.Add(MapTag(reader));
        }

        return tags;
    }

    public async Task<IReadOnlyList<Tag>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var tags = new List<Tag>();

        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        const string query = @"SELECT t.Id, t.Name, t.Color
                               FROM Tags t
                               INNER JOIN TaskTags tt ON tt.TagId = t.Id
                               WHERE tt.TaskId = @TaskId";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@TaskId", taskId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tags.Add(MapTag(reader));
        }

        return tags;
    }

    public async Task SetTaskTagsAsync(int taskId, IReadOnlyCollection<int> tagIds, CancellationToken cancellationToken = default)
    {
        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            const string deleteQuery = "DELETE FROM TaskTags WHERE TaskId = @TaskId";
            await using (var deleteCmd = new SqlCommand(deleteQuery, conn, (SqlTransaction)transaction))
            {
                deleteCmd.Parameters.AddWithValue("@TaskId", taskId);
                await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var tagId in tagIds.Distinct())
            {
                const string insertQuery = @"INSERT INTO TaskTags (TaskId, TagId)
                                             VALUES (@TaskId, @TagId)";

                await using var insertCmd = new SqlCommand(insertQuery, conn, (SqlTransaction)transaction);
                insertCmd.Parameters.AddWithValue("@TaskId", taskId);
                insertCmd.Parameters.AddWithValue("@TagId", tagId);

                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> ExistAllAsync(IReadOnlyCollection<int> tagIds, CancellationToken cancellationToken = default)
    {
        var distinctTagIds = tagIds.Distinct().ToList();
        if (distinctTagIds.Count == 0)
        {
            return true;
        }

        await using var conn = _connection.GetConnection();
        await conn.OpenAsync(cancellationToken);

        var parameterNames = new List<string>(distinctTagIds.Count);
        for (var i = 0; i < distinctTagIds.Count; i++)
        {
            parameterNames.Add($"@TagId{i}");
        }

        var query = $"SELECT COUNT(DISTINCT Id) FROM Tags WHERE Id IN ({string.Join(",", parameterNames)})";

        await using var cmd = new SqlCommand(query, conn);
        for (var i = 0; i < distinctTagIds.Count; i++)
        {
            cmd.Parameters.AddWithValue(parameterNames[i], distinctTagIds[i]);
        }

        var countObj = await cmd.ExecuteScalarAsync(cancellationToken);
        var count = countObj is null || countObj == DBNull.Value ? 0 : Convert.ToInt32(countObj);

        return count == distinctTagIds.Count;
    }

    private static Tag MapTag(SqlDataReader reader)
    {
        return new Tag
        {
            Id = (int)reader["Id"],
            Name = Convert.ToString(reader["Name"]) ?? string.Empty,
            Color = Convert.ToString(reader["Color"]) ?? "#3b82f6"
        };
    }
}
