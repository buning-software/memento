using Memento.Core.Tasks.Models;
using static Memento.Core.UnitTests.TestSupport.TestClock;

namespace Memento.Core.UnitTests.Tasks.Models;

public class TaskItemTests
{
    [Fact]
    public void EditText_is_the_original_text_when_it_was_stored()
    {
        var task = new TaskItem("1", "call Anna", null, [], Now, OriginalText: "call Anna tomorrow 3pm");

        Assert.Equal("call Anna tomorrow 3pm", task.EditText);
    }

    [Fact]
    public void EditText_is_rebuilt_from_the_parts_when_no_original_text_was_stored()
    {
        var task = new TaskItem("1", "pay rent", At("2026-10-14T16:45"), ["home", "money"], Now);

        Assert.Equal("pay rent 2026-10-14 16:45 #home #money", task.EditText);
    }

    [Fact]
    public void EditText_without_tags_or_deadline_is_just_the_title()
    {
        var task = new TaskItem("1", "buy milk", null, [], Now);

        Assert.Equal("buy milk", task.EditText);
    }

    [Fact]
    public void EditText_with_a_deadline_but_no_tags_ends_with_the_deadline()
    {
        var task = new TaskItem("1", "pay rent", At("2026-10-14T09:05"), [], Now);

        Assert.Equal("pay rent 2026-10-14 09:05", task.EditText);
    }

    [Fact]
    public void EditText_with_tags_but_no_deadline_puts_the_tags_after_the_title()
    {
        var task = new TaskItem("1", "pay rent", null, ["home"], Now);

        Assert.Equal("pay rent #home", task.EditText);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public void EditText_is_rebuilt_when_the_original_text_is_null_empty_or_blank(string? originalText)
    {
        var task = new TaskItem("1", "pay rent", null, ["home"], Now, OriginalText: originalText);

        Assert.Equal("pay rent #home", task.EditText);
    }

    [Fact]
    public void EditText_keeps_the_original_text_exactly_as_typed()
    {
        var task = new TaskItem("1", "call Anna", null, [], Now, OriginalText: "call Anna  #Work");

        Assert.Equal("call Anna  #Work", task.EditText);
    }

    [Fact]
    public void EditText_of_a_plain_task_is_its_title()
    {
        var task = new TaskItem("1", "buy milk", null, [], Now, OriginalText: "  ");

        Assert.Equal("buy milk", task.EditText);
    }
}
