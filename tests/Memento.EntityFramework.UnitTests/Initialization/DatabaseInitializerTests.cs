using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Memento.Core.Storage;
using Memento.EntityFramework.Migrations;
using Memento.EntityFramework.UnitTests.TestSupport;

namespace Memento.EntityFramework.UnitTests.Initialization;

public sealed class DatabaseInitializerTests : IDisposable
{
    private static readonly string InitialCreateId = typeof(InitialCreate).GetCustomAttribute<MigrationAttribute>()!.Id;

    private readonly TempDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private string[] AppliedMigrations()
    {
        using var context = _database.CreateContext();
        return context.Database.GetAppliedMigrations().ToArray();
    }

    [Fact]
    public void Initialize_creates_the_folder_the_tasks_table_and_its_index()
    {
        _database.Initializer.Initialize();

        Assert.True(Directory.Exists(_database.Paths.DataDirectory));
        Assert.Equal(1, _database.Count("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Tasks';"));
        Assert.Equal(1, _database.Count("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_Tasks_Status_Deadline';"));
        foreach (var column in new[] { "Id", "Title", "Deadline", "CreatedAt", "Status", "Tags", "OriginalText" })
            Assert.Equal(1, _database.Count($"SELECT COUNT(*) FROM pragma_table_info('Tasks') WHERE name = '{column}';"));
        Assert.Equal([InitialCreateId], AppliedMigrations());
    }

    [Fact]
    public void Initialize_twice_changes_nothing()
    {
        _database.Initializer.Initialize();
        _database.Scalar("INSERT INTO Tasks (Id, Title, CreatedAt) VALUES ('1', 'kept', '2026-10-01 09:00:00');");

        _database.Initializer.Initialize();

        Assert.Equal([InitialCreateId], AppliedMigrations());
        Assert.Equal(1, _database.Count("SELECT COUNT(*) FROM Tasks;"));
    }

    [Fact]
    public void Initialize_turns_on_write_ahead_logging()
    {
        _database.Initializer.Initialize();

        Assert.Equal("wal", _database.Scalar("PRAGMA journal_mode;"));
    }

    [Fact]
    public void A_file_that_is_not_a_database_surfaces_as_a_storage_exception()
    {
        Directory.CreateDirectory(_database.Paths.DataDirectory);
        File.WriteAllText(_database.Paths.DatabasePath, "this is not a SQLite database, just some text that is long enough");

        Assert.Throws<StorageException>(_database.Initializer.Initialize);
    }

    [Fact]
    public void The_model_matches_the_migrations_snapshot()
    {
        using var context = _database.CreateContext();

        Assert.False(context.Database.HasPendingModelChanges(), "Add a migration: dotnet ef migrations add <Name>");
        Assert.Equal([InitialCreateId], context.Database.GetMigrations());
    }
}
