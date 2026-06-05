using System.Data;
using Microsoft.Data.SqlClient;

namespace Dashboards_reports.CollectionTracker.Data;

public sealed class SqlConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString =
        configuration.GetConnectionString("CollectionTrackerConnection")
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Missing connection string. Add ConnectionStrings:CollectionTrackerConnection in appsettings.json.");

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
