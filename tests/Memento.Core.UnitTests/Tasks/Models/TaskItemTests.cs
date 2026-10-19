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
    public void EditText_of_a_plain_task_is_its_title()
    {
        var task = new TaskItem("1", "buy milk", null, [], Now, OriginalText: "  ");

        Assert.Equal("buy milk", task.EditText);
    }
}
