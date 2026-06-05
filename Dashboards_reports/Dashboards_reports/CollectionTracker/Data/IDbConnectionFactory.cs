using System.Data;

namespace Dashboards_reports.CollectionTracker.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
