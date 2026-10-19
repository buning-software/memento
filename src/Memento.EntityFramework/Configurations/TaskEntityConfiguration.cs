using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memento.Core.Enumerations;
using Memento.EntityFramework.Entities;

namespace Memento.EntityFramework.Configurations;

/// <summary>The Tasks table. Changing it needs a new migration.</summary>
internal sealed class TaskEntityConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id).ValueGeneratedNever();
        builder.Property(task => task.Title).IsRequired();

        // The column keeps its DEFAULT 0, but EF always writes the value it has.
        builder.Property(task => task.Status).HasDefaultValue(TaskItemStatus.Open).ValueGeneratedNever();

        builder.HasIndex(task => new { task.Status, task.Deadline }).HasDatabaseName("IX_Tasks_Status_Deadline");
    }
}
