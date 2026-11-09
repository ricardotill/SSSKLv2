using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SSSKL.Infrastructure.Persistence;

/// <summary>
/// Registers the SQL Server date functions used in <c>HasDefaultValueSql</c> on SQLite connections,
/// so column defaults keep working in the test environments without changing the SQL Server model.
/// </summary>
public sealed class SqliteFunctionsInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Register(connection);
    }

    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Register(connection);
        return Task.CompletedTask;
    }

    private static void Register(DbConnection connection)
    {
        if (connection is not SqliteConnection sqlite) return;

        sqlite.CreateFunction("GETDATE", () => DateTime.Now);
        sqlite.CreateFunction("GETUTCDATE", () => DateTime.UtcNow);
    }
}
