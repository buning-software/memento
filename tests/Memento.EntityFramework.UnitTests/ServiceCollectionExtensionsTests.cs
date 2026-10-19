using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Memento.Core.Tasks.Persistence;
using Memento.Core.UnitTests.TestSupport;
using Memento.EntityFramework.DbContext;
using Memento.EntityFramework.Initialization;
using Memento.EntityFramework.Repositories;

namespace Memento.EntityFramework.UnitTests;

public sealed class ServiceCollectionExtensionsTests : IDisposable
{
    private readonly TempDataDirectory _data = new();

    public void Dispose() => _data.Dispose();

    [Fact]
    public void Every_data_service_resolves_once_the_paths_are_provided()
    {
        using var provider = new ServiceCollection()
            .AddSingleton(_data.Paths)
            .AddMementoEntityFramework()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<TaskRepository>(provider.GetRequiredService<ITaskRepository>());
        Assert.Same(provider.GetRequiredService<ITaskRepository>(), provider.GetRequiredService<ITaskRepository>());
        Assert.NotNull(provider.GetRequiredService<DatabaseInitializer>());

        using var context = provider.GetRequiredService<IDbContextFactory<MementoDbContext>>().CreateDbContext();
        var connection = Assert.IsType<SqliteConnection>(context.Database.GetDbConnection());
        Assert.Equal(_data.Paths.DatabasePath, connection.DataSource);
        Assert.Equal(3, connection.DefaultTimeout); // a locked database fails fast instead of freezing the UI
        Assert.False(Directory.Exists(_data.Paths.DataDirectory)); // nothing touches the disk before Initialize
    }
}
