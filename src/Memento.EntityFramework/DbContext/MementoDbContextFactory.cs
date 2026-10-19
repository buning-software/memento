using Memento.EntityFramework.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Memento.EntityFramework;

/// <summary>
/// Used only by the dotnet-ef tool to build the model when adding a migration
/// (<c>dotnet ef migrations add Name --project src/Memento.EntityFramework</c>). The database is never opened.
/// </summary>
internal sealed class MementoDbContextFactory : IDesignTimeDbContextFactory<MementoDbContext>
{
    public MementoDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<MementoDbContext>().UseSqlite("Data Source=memento.db").Options);
}
