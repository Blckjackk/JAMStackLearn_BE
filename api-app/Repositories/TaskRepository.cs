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

            const string query = @"SELECT Id, ProjectId, AssigneeUserId, Title, Description, Content,
                                          Status, Priority, IsCompleted, DueDate, CreatedAt, UpdatedAt
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

            const string query = @"SELECT Id, ProjectId, AssigneeUserId, Title, Description, Content,
                                          Status, Priority, IsCompleted, DueDate, CreatedAt, UpdatedAt
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

                 const string query = @"INSERT INTO Tasks (ProjectId, AssigneeUserId, Title, Description, Content, Status, Priority, IsCompleted, DueDate, CreatedAt, UpdatedAt)
                            OUTPUT INSERTED.Id, INSERTED.ProjectId, INSERTED.AssigneeUserId, INSERTED.Title,
                                INSERTED.Description, INSERTED.Content, INSERTED.Status, INSERTED.Priority,
                                INSERTED.IsCompleted, INSERTED.DueDate, INSERTED.CreatedAt, INSERTED.UpdatedAt
                            VALUES (@ProjectId, @AssigneeUserId, @Title, @Description, @Content, @Status, @Priority, @IsCompleted, @DueDate, @CreatedAt, @UpdatedAt)";

            await using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@ProjectId", task.ProjectId);
            cmd.Parameters.AddWithValue("@AssigneeUserId", (object?)task.AssigneeUserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Title", task.Title);
            cmd.Parameters.AddWithValue("@Description", task.Description);
            cmd.Parameters.AddWithValue("@Content", task.Content);
            cmd.Parameters.AddWithValue("@Status", task.Status);
            cmd.Parameters.AddWithValue("@Priority", task.Priority);
            cmd.Parameters.AddWithValue("@IsCompleted", task.IsCompleted);
            cmd.Parameters.AddWithValue("@DueDate", (object?)task.DueDate?.UtcDateTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", task.CreatedAt);
            cmd.Parameters.AddWithValue("@UpdatedAt", task.UpdatedAt);

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
                                   SET AssigneeUserId = @AssigneeUserId,
                                       Title = @Title,
                                       Description = @Description,
                                       Content = @Content,
                                       Status = @Status,
                                       Priority = @Priority,
                                       IsCompleted = @IsCompleted,
                                       DueDate = @DueDate,
                                       UpdatedAt = @UpdatedAt
                                   WHERE Id = @Id";

            await using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Id", task.Id);
            cmd.Parameters.AddWithValue("@AssigneeUserId", (object?)task.AssigneeUserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Title", task.Title);
            cmd.Parameters.AddWithValue("@Description", task.Description);
            cmd.Parameters.AddWithValue("@Content", task.Content);
            cmd.Parameters.AddWithValue("@Status", task.Status);
            cmd.Parameters.AddWithValue("@Priority", task.Priority);
            cmd.Parameters.AddWithValue("@IsCompleted", task.IsCompleted);
            cmd.Parameters.AddWithValue("@DueDate", (object?)task.DueDate?.UtcDateTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdatedAt", task.UpdatedAt);

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
                AssigneeUserId = reader["AssigneeUserId"] == DBNull.Value ? null : (int)reader["AssigneeUserId"],
                Title = Convert.ToString(reader["Title"]) ?? string.Empty,
                Description = Convert.ToString(reader["Description"]) ?? string.Empty,
                Content = Convert.ToString(reader["Content"]) ?? string.Empty,
                Status = Convert.ToString(reader["Status"]) ?? "TODO",
                Priority = Convert.ToString(reader["Priority"]) ?? "Medium",
                IsCompleted = (bool)reader["IsCompleted"],
                DueDate = MapDueDate(reader["DueDate"]),
                CreatedAt = reader["CreatedAt"] == DBNull.Value ? DateTime.UtcNow : (DateTime)reader["CreatedAt"],
                UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? DateTime.UtcNow : (DateTime)reader["UpdatedAt"]
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
