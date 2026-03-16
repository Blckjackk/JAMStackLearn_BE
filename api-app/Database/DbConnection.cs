using Microsoft.Data.SqlClient;

namespace api_app.Database;

public class DbConnection
{
    private readonly string _connectionString;

    public DbConnection(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
    }

    public SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
