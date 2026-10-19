using Memento.EntityFramework.Configurations;
using Memento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace Memento.EntityFramework.DbContext;

/// <summary>The local SQLite database. Short-lived: create one per operation through IDbContextFactory.</summary>
public sealed class MementoDbContext(DbContextOptions<MementoDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new TaskEntityConfiguration());
}
