using Memento.Enumerations;
using Memento.Overlay.ViewModels;
using Memento.UnitTests.TestSupport;

namespace Memento.UnitTests.Overlay.ViewModels;

public class OverlayViewModelNavigationTests
{
    private readonly OverlayFixture _fixture = new();

    private OverlayViewModel CreateWithTasks(int count)
    {
        for (int i = 0; i < count; i++)
            _fixture.AddTask($"task {i}");
        return _fixture.CreateViewModel();
    }

    [Fact]
    public void Down_walks_the_tasks_then_the_settings_button_then_wraps_to_the_input()
    {
        var viewModel = CreateWithTasks(3);
        var visited = new List<(int, OverlayFocus)>();

        for (int i = 0; i < 5; i++)
        {
            viewModel.HandleKey(OverlayKey.Down);
            visited.Add((viewModel.SelectedIndex, viewModel.Focus));
        }

        Assert.Equal(
            new[]
            {
                (0, OverlayFocus.Input), (1, OverlayFocus.Input), (2, OverlayFocus.Input),
                (-1, OverlayFocus.Settings), (-1, OverlayFocus.Input),
            },
            visited);
    }

    [Fact]
    public void Up_from_the_input_goes_to_the_settings_button_then_the_last_task()
    {
        var viewModel = CreateWithTasks(3);

        viewModel.HandleKey(OverlayKey.Up);
        Assert.Equal(OverlayFocus.Settings, viewModel.Focus);

        viewModel.HandleKey(OverlayKey.Up);
        Assert.Equal(2, viewModel.SelectedIndex);
        Assert.Equal(OverlayFocus.Input, viewModel.Focus);
    }

    [Fact]
    public void Up_walks_back_through_the_tasks_to_the_input()
    {
        var viewModel = CreateWithTasks(2);
        viewModel.SelectedIndex = 1;

        viewModel.HandleKey(OverlayKey.Up);
        Assert.Equal((0, OverlayFocus.Input), (viewModel.SelectedIndex, viewModel.Focus));

        viewModel.HandleKey(OverlayKey.Up);
        Assert.Equal((-1, OverlayFocus.Input), (viewModel.SelectedIndex, viewModel.Focus));
    }

    [Fact]
    public void Down_with_a_single_task_cycles_through_three_stops()
    {
        var viewModel = CreateWithTasks(1);

        viewModel.HandleKey(OverlayKey.Down);
        Assert.Equal((0, OverlayFocus.Input), (viewModel.SelectedIndex, viewModel.Focus));
        viewModel.HandleKey(OverlayKey.Down);
        Assert.Equal((-1, OverlayFocus.Settings), (viewModel.SelectedIndex, viewModel.Focus));
        viewModel.HandleKey(OverlayKey.Down);
        Assert.Equal((-1, OverlayFocus.Input), (viewModel.SelectedIndex, viewModel.Focus));
    }

    [Fact]
    public void Without_tasks_up_toggles_between_input_and_settings_too()
    {
        var viewModel = CreateWithTasks(0);

        Assert.True(viewModel.HandleKey(OverlayKey.Up));
        Assert.Equal((-1, OverlayFocus.Settings), (viewModel.SelectedIndex, viewModel.Focus));

        Assert.True(viewModel.HandleKey(OverlayKey.Up));
        Assert.Equal((-1, OverlayFocus.Input), (viewModel.SelectedIndex, viewModel.Focus));
    }

    [Fact]
    public void Tab_from_the_settings_button_wraps_to_the_input_and_shift_tab_goes_to_the_last_task()
    {
        var viewModel = CreateWithTasks(2);
        viewModel.SettingsFocusChanged(focused: true);

        viewModel.HandleKey(OverlayKey.ShiftTab);
        Assert.Equal((1, OverlayFocus.Input), (viewModel.SelectedIndex, viewModel.Focus));

        viewModel.SettingsFocusChanged(focused: true);
        viewModel.HandleKey(OverlayKey.Tab);
        Assert.Equal((-1, OverlayFocus.Input), (viewModel.SelectedIndex, viewModel.Focus));
    }

    [Fact]
    public void A_key_the_overlay_does_not_know_is_left_to_the_window()
    {
        var viewModel = CreateWithTasks(1);

        Assert.False(viewModel.HandleKey((OverlayKey)999));
        Assert.Equal((-1, OverlayFocus.Input), (viewModel.SelectedIndex, viewModel.Focus));
    }

    [Fact]
    public void Navigation_does_not_clear_what_was_typed()
    {
        var viewModel = CreateWithTasks(2);
        viewModel.InputText = "half a line";

        viewModel.HandleKey(OverlayKey.Down);
        viewModel.HandleKey(OverlayKey.Up);

        Assert.Equal("half a line", viewModel.InputText);
    }

    [Fact]
    public void Tab_and_shift_tab_move_like_down_and_up()
    {
        var viewModel = CreateWithTasks(2);

        Assert.True(viewModel.HandleKey(OverlayKey.Tab));
        Assert.Equal(0, viewModel.SelectedIndex);

        Assert.True(viewModel.HandleKey(OverlayKey.ShiftTab));
        Assert.Equal(-1, viewModel.SelectedIndex);
        Assert.Equal(OverlayFocus.Input, viewModel.Focus);
    }

    [Fact]
    public void Without_tasks_navigation_toggles_between_input_and_settings()
    {
        var viewModel = CreateWithTasks(0);

        viewModel.HandleKey(OverlayKey.Down);
        Assert.Equal(OverlayFocus.Settings, viewModel.Focus);

        viewModel.HandleKey(OverlayKey.Down);
        Assert.Equal(OverlayFocus.Input, viewModel.Focus);
    }

    [Fact]
    public void Enter_on_the_settings_button_is_left_to_the_button()
    {
        var viewModel = CreateWithTasks(1);
        viewModel.HandleKey(OverlayKey.Up);

        Assert.False(viewModel.HandleKey(OverlayKey.Enter));
        Assert.False(viewModel.HandleKey(OverlayKey.F2));
    }

    [Fact]
    public void Space_on_the_settings_button_is_left_to_the_button()
    {
        var viewModel = CreateWithTasks(1);
        viewModel.HandleKey(OverlayKey.Up);

        Assert.False(viewModel.HandleKey(OverlayKey.Space));
        Assert.Single(viewModel.Tasks);
    }

    [Fact]
    public void Escape_asks_to_close_the_overlay()
    {
        var viewModel = CreateWithTasks(0);
        int closeRequests = 0;
        viewModel.CloseRequested += (_, _) => closeRequests++;

        Assert.True(viewModel.HandleKey(OverlayKey.Escape));
        Assert.Equal(1, closeRequests);
    }

    [Fact]
    public void RequestSettings_raises_SettingsRequested()
    {
        var viewModel = CreateWithTasks(0);
        int requests = 0;
        viewModel.SettingsRequested += (_, _) => requests++;

        viewModel.RequestSettings();

        Assert.Equal(1, requests);
    }

    [Fact]
    public void The_hints_follow_the_mode()
    {
        var viewModel = CreateWithTasks(1);
        Assert.Equal(("save", "close", false), (viewModel.EnterHint, viewModel.EscapeHint, viewModel.ShowEditHint));

        viewModel.SelectedIndex = 0;
        Assert.Equal(("complete", "close", true), (viewModel.EnterHint, viewModel.EscapeHint, viewModel.ShowEditHint));

        viewModel.SettingsFocusChanged(focused: true);
        Assert.Equal("open", viewModel.EnterHint);
    }

    [Fact]
    public void Changing_the_selection_notifies_the_hints()
    {
        var viewModel = CreateWithTasks(1);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.SelectedIndex = 0;

        Assert.Contains(nameof(OverlayViewModel.EnterHint), changed);
        Assert.Contains(nameof(OverlayViewModel.ShowEditHint), changed);
    }

    [Fact]
    public void Focus_leaving_the_settings_button_returns_to_the_input()
    {
        var viewModel = CreateWithTasks(0);
        viewModel.SettingsFocusChanged(focused: true);

        viewModel.SettingsFocusChanged(focused: false);

        Assert.Equal(OverlayFocus.Input, viewModel.Focus);
    }

    [Fact]
    public void Focus_leaving_a_button_that_never_had_it_keeps_the_current_focus()
    {
        var viewModel = CreateWithTasks(1);
        viewModel.SelectedIndex = 0;

        viewModel.SettingsFocusChanged(focused: false);

        Assert.Equal(OverlayFocus.Input, viewModel.Focus);
        Assert.Equal(0, viewModel.SelectedIndex);
    }

    [Fact]
    public void Showing_the_overlay_clears_an_error()
    {
        var viewModel = CreateWithTasks(0);
        viewModel.InputText = "buy milk";
        _fixture.Repository.Fail = true;
        viewModel.HandleKey(OverlayKey.Enter);
        Assert.NotEmpty(viewModel.ErrorText);

        viewModel.OnShown();

        Assert.Empty(viewModel.ErrorText);
    }

    [Fact]
    public void Showing_the_overlay_keeps_the_typed_text()
    {
        var viewModel = CreateWithTasks(0);
        viewModel.InputText = "unfinished";

        viewModel.OnShown();

        Assert.Equal("unfinished", viewModel.InputText);
    }

    [Fact]
    public void Showing_the_overlay_starts_fresh_on_the_input()
    {
        var viewModel = CreateWithTasks(2);
        viewModel.SelectedIndex = 1;
        viewModel.SettingsFocusChanged(focused: true);

        viewModel.OnShown();

        Assert.Equal(-1, viewModel.SelectedIndex);
        Assert.Equal(OverlayFocus.Input, viewModel.Focus);
    }
}
