using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Memento.Core.Storage;
using Memento.Core.Tasks.Persistence;
using Memento.EntityFramework.DbContext;
using Memento.EntityFramework.Initialization;
using Memento.EntityFramework.Interceptors;
using Memento.EntityFramework.Repositories;

namespace Memento.EntityFramework;

public static class ServiceCollectionExtensions
{
    /// <summary>How long a command waits for a locked database before it fails, so the UI never hangs for long.</summary>
    internal const int TimeoutSeconds = 3;

    /// <summary>
    /// Registers the SQLite data layer: a <see cref="MementoDbContext"/> factory on <see cref="MementoPaths.DatabasePath"/>,
    /// <see cref="ITaskRepository"/> and <see cref="DatabaseInitializer"/>.
    /// The host must register <see cref="MementoPaths"/> (AddMementoCore does).
    /// </summary>
    public static IServiceCollection AddMementoEntityFramework(this IServiceCollection services)
    {
        services.AddDbContextFactory<MementoDbContext>((provider, options) => options
            .UseSqlite(new SqliteConnectionStringBuilder
            {
                DataSource = provider.GetRequiredService<MementoPaths>().DatabasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                DefaultTimeout = TimeoutSeconds,
            }.ToString())
            .AddInterceptors(new SynchronousPragmaInterceptor()));

        services.AddSingleton<ITaskRepository, TaskRepository>();
        services.AddSingleton<DatabaseInitializer>();

        return services;
    }
}
