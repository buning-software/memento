using Microsoft.EntityFrameworkCore;
using Memento.Core.Storage;
using Memento.EntityFramework.DbContext;
using Memento.EntityFramework.Internal;

namespace Memento.EntityFramework.Initialization;

/// <summary>
/// Prepares the database before first use: creates the folder, turns on WAL and applies the migrations.
/// To change the schema, add a migration with the dotnet-ef tool.
/// </summary>
public sealed class DatabaseInitializer(MementoPaths paths, IDbContextFactory<MementoDbContext> contexts)
{
    /// <exception cref="StorageException">The database cannot be opened or migrated.</exception>
    public void Initialize()
    {
        Directory.CreateDirectory(paths.DataDirectory);
        StorageErrors.Wrap(() =>
        {
            using var context = contexts.CreateDbContext();

            // WAL is persisted in the file: fast writes that do not block readers.
            context.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
            context.Database.Migrate();
        });
    }
}
