using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Memento.Core.Storage;
using Memento.Core.Tasks.Persistence;
using Memento.Core.UnitTests.TestSupport;
using Memento.EntityFramework.DbContext;
using Memento.EntityFramework.Initialization;

namespace Memento.EntityFramework.UnitTests.TestSupport;

/// <summary>
/// The real data layer, wired as the app wires it, on a database in a temp folder of its own.
/// The raw SQL helpers inspect the file without going through EF Core.
/// </summary>
internal sealed class TempDatabase : IDisposable
{
    private readonly TempDataDirectory _directory = new();
    private readonly ServiceProvider _services;

    public TempDatabase()
    {
        _services = new ServiceCollection()
            .AddSingleton(_directory.Paths)
            .AddMementoEntityFramework()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public MementoPaths Paths => _directory.Paths;

    public DatabaseInitializer Initializer => _services.GetRequiredService<DatabaseInitializer>();

    public ITaskRepository Repository => _services.GetRequiredService<ITaskRepository>();

    public MementoDbContext CreateContext() =>
        _services.GetRequiredService<IDbContextFactory<MementoDbContext>>().CreateDbContext();

    /// <summary>Runs SQL directly on the file and returns the first column of the first row.</summary>
    public object? Scalar(string sql)
    {
        using var connection = new SqliteConnection($"Data Source={Paths.DatabasePath};Mode=ReadWrite");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    public long Count(string sql) => Convert.ToInt64(Scalar(sql), CultureInfo.InvariantCulture);

    public void Dispose()
    {
        _services.Dispose();
        SqliteConnection.ClearAllPools(); // pooled connections keep the file locked
        _directory.Dispose();
    }
}
