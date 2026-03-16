using api_app.Database;
using api_app.Models;
using api_app.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace api_app.Repositories
{
    public class TaskRepository : ITaskRepository
    {

        private readonly DbConnection _connection;

        public TaskRepository(DbConnection connection)
        {
            _connection = connection;
        }

        public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            await using var conn = _connection.GetConnection();
            await conn.OpenAsync(cancellationToken);

            const string query = @"SELECT Id, ProjectId, Title, Content, IsCompleted, DueDate
                                   FROM Tasks
                                   WHERE Id = @Id";

            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return MapTask(reader);
            }

            return null;
        }

        public async Task<IReadOnlyList<TaskItem>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            var tasks = new List<TaskItem>();

            await using var conn = _connection.GetConnection();
            await conn.OpenAsync(cancellationToken);

            const string query = @"SELECT Id, ProjectId, Title, Content, IsCompleted, DueDate
                                   FROM Tasks
                                   WHERE ProjectId = @ProjectId";

            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProjectId", projectId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                tasks.Add(MapTask(reader));
            }

            return tasks;
        }

        public async Task<TaskItem> CreateAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            await using var conn = _connection.GetConnection();
            await conn.OpenAsync(cancellationToken);

            const string query = @"INSERT INTO Tasks (ProjectId, Title, Content, IsCompleted, DueDate)
                                   OUTPUT INSERTED.Id, INSERTED.ProjectId, INSERTED.Title, INSERTED.Content, INSERTED.IsCompleted, INSERTED.DueDate
                                   VALUES (@ProjectId, @Title, @Content, @IsCompleted, @DueDate)";

            await using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@ProjectId", task.ProjectId);
            cmd.Parameters.AddWithValue("@Title", task.Title);
            cmd.Parameters.AddWithValue("@Content", (object?)task.Content ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsCompleted", task.IsCompleted);
            cmd.Parameters.AddWithValue("@DueDate", (object?)task.DueDate?.UtcDateTime ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Failed to create task.");
            }

            return MapTask(reader);
        }

        public async Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            await using var conn = _connection.GetConnection();
            await conn.OpenAsync(cancellationToken);

            const string query = @"UPDATE Tasks
                                   SET Title = @Title,
                                       Content = @Content,
                                       IsCompleted = @IsCompleted,
                                       DueDate = @DueDate
                                   WHERE Id = @Id";

            await using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Id", task.Id);
            cmd.Parameters.AddWithValue("@Title", task.Title);
            cmd.Parameters.AddWithValue("@Content", (object?)task.Content ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsCompleted", task.IsCompleted);
            cmd.Parameters.AddWithValue("@DueDate", (object?)task.DueDate?.UtcDateTime ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            await using var conn = _connection.GetConnection();
            await conn.OpenAsync(cancellationToken);

            const string query = "DELETE FROM Tasks WHERE Id = @Id";

            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return rows > 0;
        }

        private static TaskItem MapTask(SqlDataReader reader)
        {
            return new TaskItem
            {
                Id = (int)reader["Id"],
                ProjectId = (int)reader["ProjectId"],
                Title = Convert.ToString(reader["Title"]) ?? string.Empty,
                Content = reader["Content"] == DBNull.Value ? null : Convert.ToString(reader["Content"]),
                IsCompleted = (bool)reader["IsCompleted"],
                DueDate = MapDueDate(reader["DueDate"])
            };
        }

        private static DateTimeOffset? MapDueDate(object dueDateValue)
        {
            return dueDateValue switch
            {
                DBNull => null,
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
                _ => null
            };
        }
    }
}
