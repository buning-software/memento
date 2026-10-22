using Memento.Core.Enumerations;
using Memento.Core.Tasks.Models;
using Memento.EntityFramework.Entities;
using Memento.EntityFramework.Mapping;
using static Memento.Core.UnitTests.TestSupport.TestClock;

namespace Memento.EntityFramework.UnitTests.Mapping;

public sealed class TaskMapperTests
{
    [Fact]
    public void ToEntity_copies_every_field_and_joins_the_tags()
    {
        var item = new TaskItem("id-1", "call Anna", Now.AddDays(1), ["work", "phone"], Now, TaskItemStatus.Completed, "call Anna #work #phone");

        var entity = TaskMapper.ToEntity(item);

        Assert.Equal("id-1", entity.Id);
        Assert.Equal("call Anna", entity.Title);
        Assert.Equal(Now.AddDays(1), entity.Deadline);
        Assert.Equal(Now, entity.CreatedAt);
        Assert.Equal(TaskItemStatus.Completed, entity.Status);
        Assert.Equal("work,phone", entity.Tags);
        Assert.Equal("call Anna #work #phone", entity.OriginalText);
    }

    [Fact]
    public void ToEntity_stores_no_tags_as_null()
    {
        var entity = TaskMapper.ToEntity(new TaskItem("id-1", "buy milk", null, [], Now));

        Assert.Null(entity.Tags);
        Assert.Null(entity.Deadline);
        Assert.Null(entity.OriginalText);
    }

    [Fact]
    public void ToItem_copies_every_field_and_splits_the_tags()
    {
        var entity = new TaskEntity
        {
            Id = "id-1",
            Title = "call Anna",
            Deadline = Now.AddDays(1),
            CreatedAt = Now,
            Status = TaskItemStatus.Deleted,
            Tags = "work,,phone",
            OriginalText = "call Anna",
        };

        var item = TaskMapper.ToItem(entity);

        Assert.Equal(new TaskItem("id-1", "call Anna", Now.AddDays(1), item.Tags, Now, TaskItemStatus.Deleted, "call Anna"), item);
        Assert.Equal(["work", "phone"], item.Tags);
    }

    [Fact]
    public void ToItem_reads_null_tags_as_none()
    {
        var item = TaskMapper.ToItem(new TaskEntity { Id = "id-1", Title = "buy milk", CreatedAt = Now });

        Assert.Empty(item.Tags);
        Assert.Equal(TaskItemStatus.Open, item.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData(",")]
    [InlineData(",,")]
    public void ToItem_reads_empty_or_separator_only_tags_as_none(string tags)
    {
        var item = TaskMapper.ToItem(new TaskEntity { Id = "id-1", Title = "buy milk", CreatedAt = Now, Tags = tags });

        Assert.Empty(item.Tags);
    }

    [Fact]
    public void ToItem_reads_a_single_tag()
    {
        var item = TaskMapper.ToItem(new TaskEntity { Id = "id-1", Title = "buy milk", CreatedAt = Now, Tags = "home" });

        Assert.Equal(["home"], item.Tags);
    }

    [Theory]
    [InlineData(TaskItemStatus.Open)]
    [InlineData(TaskItemStatus.Completed)]
    [InlineData(TaskItemStatus.Deleted)]
    public void Every_status_survives_the_mapping_in_both_directions(TaskItemStatus status)
    {
        var item = new TaskItem("id-1", "x", null, [], Now, status);

        Assert.Equal(status, TaskMapper.ToEntity(item).Status);
        Assert.Equal(status, TaskMapper.ToItem(TaskMapper.ToEntity(item)).Status);
    }

    [Fact]
    public void A_missing_original_text_stays_null_in_both_directions()
    {
        var item = new TaskItem("id-1", "buy milk", null, [], Now, OriginalText: null);

        Assert.Null(TaskMapper.ToEntity(item).OriginalText);
        Assert.Null(TaskMapper.ToItem(TaskMapper.ToEntity(item)).OriginalText);
    }

    [Fact]
    public void An_empty_original_text_is_not_turned_into_null()
    {
        var entity = TaskMapper.ToEntity(new TaskItem("id-1", "buy milk", null, [], Now, OriginalText: ""));

        Assert.Equal("", entity.OriginalText);
    }

    [Fact]
    public void A_task_without_deadline_or_tags_survives_the_round_trip()
    {
        var original = new TaskItem("id-1", "buy milk", null, [], Now);

        var item = TaskMapper.ToItem(TaskMapper.ToEntity(original));

        Assert.Null(item.Deadline);
        Assert.Empty(item.Tags);
        Assert.Equal(original, item with { Tags = original.Tags });
    }

    [Fact]
    public void A_task_survives_the_round_trip()
    {
        var original = new TaskItem("id-1", "call Anna", Now.AddDays(1), ["work"], Now, TaskItemStatus.Open, "call Anna #work");

        var item = TaskMapper.ToItem(TaskMapper.ToEntity(original));

        Assert.Equal(original.Tags, item.Tags);
        Assert.Equal(original, item with { Tags = original.Tags });
    }
}
