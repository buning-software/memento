using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Memento.Core.Storage;

namespace Memento.EntityFramework.Internal;

/// <summary>Turns SQLite and EF Core failures into <see cref="StorageException"/>, so callers never depend on them.</summary>
internal static class StorageErrors
{
    public static T Wrap<T>(Func<T> work)
    {
        try
        {
            return work();
        }
        catch (Exception ex) when (ex is SqliteException or DbUpdateException or FormatException or InvalidOperationException)
        {
            throw new StorageException($"The task database could not be used: {ex.GetBaseException().Message}", ex);
        }
    }

    public static void Wrap(Action work) => Wrap(() =>
    {
        work();
        return 0;
    });
}
