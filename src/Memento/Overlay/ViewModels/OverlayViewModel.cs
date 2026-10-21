using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Memento.Core.Enumerations;
using Memento.Core.Storage;
using Memento.Core.Tasks.Services;
using Memento.Enumerations;
using Memento.Overlay.Tasks.Models;
using Memento.Overlay.Tasks.Services;
using Memento.Overlay.Tasks.ViewModels;

namespace Memento.Overlay.ViewModels;

/// <summary>
/// The quick-capture overlay: an input line with a live preview, and the open tasks below it.
/// Enter saves the typed task, or completes the selected one (Space completes it too); F2 edits the selected one in place.
/// Up/Down and Tab/Shift+Tab walk one wrapping list: the input (0), the tasks (1..N), then the Settings button (N+1).
/// </summary>
public sealed partial class OverlayViewModel : ObservableObject
{
    /// <summary>Maximum number of tasks listed below the input.</summary>
    public const int MaxListedTasks = 50;

    private readonly TaskService _tasks;
    private readonly DeadlineFormatter _deadlines;
    private TaskRowViewModel? _editingRow;

    public OverlayViewModel(TaskService tasks, DeadlineFormatter deadlines)
    {
        _tasks = tasks;
        _deadlines = deadlines;
    }

    public event EventHandler? CloseRequested;

    public event EventHandler? SettingsRequested;

    public ObservableCollection<TaskRowViewModel> Tasks { get; } = [];

    public ObservableCollection<PreviewBadge> Preview { get; } = [];

    [ObservableProperty]
    public partial string InputText { get; set; } = "";

    /// <summary>-1 = no task selected: Enter saves the typed line.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnterHint), nameof(ShowEditHint))]
    public partial int SelectedIndex { get; set; } = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnterHint))]
    public partial OverlayFocus Focus { get; set; } = OverlayFocus.Input;

    /// <summary>"Add a title" when the line holds only a deadline and/or tags; otherwise null.</summary>
    [ObservableProperty]
    public partial string? PreviewHint { get; set; }

    /// <summary>
    /// Why the last save, edit or completion failed; empty when there is nothing to report. Shown inline rather than
    /// in a dialog: a modal dialog would deactivate the overlay, which hides it and drops the edit.
    /// Cleared by the next successful action and whenever the typed or edited line changes.
    /// </summary>
    [ObservableProperty]
    public partial string ErrorText { get; set; } = "";

    public bool HasTasks => Tasks.Count > 0;

    public bool IsEditing => _editingRow is not null;

    /// <summary>What Enter does right now, shown in the hint bar.</summary>
    public string EnterHint =>
        Focus == OverlayFocus.Settings ? "open"
        : IsEditing ? "save"
        : SelectedRow is not null ? "complete"
        : "save";

    public string EscapeHint => IsEditing ? "cancel" : "close";

    /// <summary>The F2 chip shows while a task is selected and not already being edited.</summary>
    public bool ShowEditHint => SelectedRow is not null && !IsEditing;

    private TaskRowViewModel? SelectedRow =>
        SelectedIndex >= 0 && SelectedIndex < Tasks.Count ? Tasks[SelectedIndex] : null;

    /// <summary>
    /// Handles a key pressed in the overlay. False when the key is not the overlay's to handle:
    /// Enter (and F2) on the focused Settings button belong to the button.
    /// </summary>
    public bool HandleKey(OverlayKey key)
    {
        if (IsEditing)
        {
            if (key == OverlayKey.Enter)
                SaveEdit();
            else if (key == OverlayKey.Escape)
                CancelEdit();
            return key != OverlayKey.Space; // navigation and F2 are blocked while editing; typing (Space too) goes to the editor
        }

        if (Focus == OverlayFocus.Settings && key is OverlayKey.Enter or OverlayKey.F2)
            return false;

        switch (key)
        {
            case OverlayKey.Escape:
                CloseRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case OverlayKey.Down or OverlayKey.Tab:
                Navigate(+1);
                return true;
            case OverlayKey.Up or OverlayKey.ShiftTab:
                Navigate(-1);
                return true;
            case OverlayKey.Enter:
                Submit();
                return true;
            case OverlayKey.Space:
                // Completes only a selected task; otherwise Space is a typed space, or the Settings button's click.
                if (Focus != OverlayFocus.Input || SelectedRow is not { } selected)
                    return false;
                Complete(selected);
                return true;
            case OverlayKey.F2:
                if (SelectedRow is { } row)
                    BeginEdit(row);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Reloads the open tasks; an edit in progress ends first. A storage failure hides the list.</summary>
    public void Refresh()
    {
        CancelEdit();
        SelectedIndex = -1;
        Tasks.Clear();
        try
        {
            foreach (var task in _tasks.GetOpen(MaxListedTasks))
                Tasks.Add(new TaskRowViewModel(task, _deadlines));
        }
        catch (StorageException)
        {
            Tasks.Clear();
        }
        OnPropertyChanged(nameof(HasTasks));
    }

    /// <summary>The overlay was just shown: start fresh on the input.</summary>
    public void OnShown()
    {
        SelectedIndex = -1;
        Focus = OverlayFocus.Input;
        ErrorText = "";
    }

    public void RequestSettings() => SettingsRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>The window reports focus moving onto or off the Settings button (by keyboard or mouse).</summary>
    public void SettingsFocusChanged(bool focused)
    {
        if (focused)
            Focus = OverlayFocus.Settings;
        else if (Focus == OverlayFocus.Settings)
            Focus = OverlayFocus.Input;
    }

    /// <summary>Leaves edit mode without saving. Also called when the editor loses focus and when the overlay hides.</summary>
    public void CancelEdit()
    {
        if (_editingRow is not { } row)
            return;

        _editingRow = null;
        row.PropertyChanged -= EditingRow_PropertyChanged;
        row.IsEditing = false;
        ErrorText = "";
        UpdatePreview(InputText);
        Focus = OverlayFocus.Input;
        NotifyEditStateChanged();
    }

    partial void OnInputTextChanged(string value)
    {
        SelectedIndex = -1; // typing means "new task" again
        ErrorText = "";
        UpdatePreview(value);
    }

    private void Navigate(int delta)
    {
        int stops = Tasks.Count + 2;
        int position = Focus == OverlayFocus.Settings ? Tasks.Count + 1 : SelectedIndex + 1;
        int target = (position + delta + stops) % stops;

        if (target == Tasks.Count + 1)
        {
            SelectedIndex = -1;
            Focus = OverlayFocus.Settings;
        }
        else
        {
            SelectedIndex = target - 1; // position 0 = no selection = the input
            Focus = OverlayFocus.Input;
        }
    }

    private void Submit()
    {
        if (SelectedRow is { } row)
            Complete(row);
        else
            Capture();
    }

    /// <summary>Saves the typed line and keeps the overlay open for the next one. On failure the text stays.</summary>
    private void Capture()
    {
        try
        {
            if (_tasks.Capture(InputText) != TaskChange.Saved)
                return; // only a deadline and/or tags: nothing to save
        }
        catch (StorageException ex)
        {
            ErrorText = $"Could not save the task. {ex.Message}";
            return;
        }

        ErrorText = "";
        InputText = "";
        Refresh();
    }

    /// <summary>Completing removes the task from the list; the selection stays in place to work through the list.</summary>
    private void Complete(TaskRowViewModel row)
    {
        int index = SelectedIndex;
        try
        {
            _tasks.Complete(row.Item.Id);
        }
        catch (StorageException ex)
        {
            ErrorText = $"Could not update the task. {ex.Message}";
            return;
        }

        ErrorText = "";
        Refresh();
        SelectedIndex = Math.Min(index, Tasks.Count - 1);
    }

    /// <summary>Edits the row in place, starting from the line as it was typed, so deadline and tags can change too.</summary>
    private void BeginEdit(TaskRowViewModel row)
    {
        _editingRow = row;
        row.EditText = row.Item.EditText;
        row.IsEditing = true;
        row.PropertyChanged += EditingRow_PropertyChanged;
        ErrorText = "";
        UpdatePreview(row.EditText);
        Focus = OverlayFocus.Editor;
        NotifyEditStateChanged();
    }

    /// <summary>A line without a title keeps editing; a storage failure shows an inline error and keeps editing.</summary>
    private void SaveEdit()
    {
        if (_editingRow is not { } row)
            return;

        TaskChange change;
        try
        {
            change = _tasks.Edit(row.Item, row.EditText);
        }
        catch (StorageException ex)
        {
            ErrorText = $"Could not update the task. {ex.Message}";
            return;
        }

        if (change == TaskChange.MissingTitle)
            return;

        ErrorText = "";
        string id = row.Item.Id;
        Refresh(); // ends the edit
        SelectedIndex = Tasks.ToList().FindIndex(r => r.Item.Id == id);
    }

    private void EditingRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TaskRowViewModel.EditText) && sender is TaskRowViewModel row)
        {
            ErrorText = "";
            UpdatePreview(row.EditText);
        }
    }

    private void NotifyEditStateChanged()
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(EnterHint));
        OnPropertyChanged(nameof(EscapeHint));
        OnPropertyChanged(nameof(ShowEditHint));
    }

    /// <summary>Shows what the parser makes of the line: the deadline and the tags as badges.</summary>
    private void UpdatePreview(string text)
    {
        Preview.Clear();
        PreviewHint = null;
        if (string.IsNullOrWhiteSpace(text))
            return;

        var parsed = _tasks.Preview(text);
        if (parsed.Title.Length == 0)
            PreviewHint = "Add a title";
        if (parsed.Deadline is { } deadline)
            Preview.Add(new PreviewBadge(PreviewBadgeKind.Deadline, _deadlines.Format(deadline)));
        foreach (var tag in parsed.Tags)
            Preview.Add(new PreviewBadge(PreviewBadgeKind.Tag, tag));
    }
}
