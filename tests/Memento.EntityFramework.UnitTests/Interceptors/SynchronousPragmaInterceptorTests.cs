using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Memento.EntityFramework.UnitTests.TestSupport;

namespace Memento.EntityFramework.UnitTests.Interceptors;

public sealed class SynchronousPragmaInterceptorTests : IDisposable
{
    private const long Normal = 1; // SQLite's default is FULL (2)

    private readonly TempDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public void Every_connection_the_context_opens_runs_with_synchronous_normal()
    {
        _database.Initializer.Initialize();
        using var context = _database.CreateContext();

        context.Database.OpenConnection();
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA synchronous;";

        Assert.Equal(Normal, Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Connections_opened_asynchronously_run_with_synchronous_normal_too()
    {
        _database.Initializer.Initialize();
        await using var context = _database.CreateContext();

        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA synchronous;";

        Assert.Equal(Normal, Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), CultureInfo.InvariantCulture));
    }
}
