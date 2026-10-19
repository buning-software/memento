using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Memento.EntityFramework.Interceptors;

/// <summary>
/// Sets PRAGMA synchronous = NORMAL on every opened connection (the setting is per connection).
/// With WAL, NORMAL is safe and keeps writes fast enough for the UI thread.
/// </summary>
internal sealed class SynchronousPragmaInterceptor : DbConnectionInterceptor
{
    private const string Pragma = "PRAGMA synchronous = NORMAL;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Pragma;
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = Pragma;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
