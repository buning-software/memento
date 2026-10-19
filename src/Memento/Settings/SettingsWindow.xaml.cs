using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using Memento.Hotkeys;
using Memento.Settings.ViewModels;
using Memento.Theme;

namespace Memento.Settings;

/// <summary>The settings window. Behaviour lives in <see cref="SettingsViewModel"/>; this class only records key presses.</summary>
public partial class SettingsWindow : FluentWindow
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel, ThemeController theme)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        theme.Attach(this);
    }

    private void ShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.StartRecording();
        ((UIElement)sender).Focus();
    }

    private void ShortcutButton_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_viewModel.IsRecordingShortcut)
            return;

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key; // Alt combinations arrive as Key.System

        if (key == Key.Escape)
            _viewModel.StopRecording();
        else if (HotkeyText.FromKeyPress(key, Keyboard.Modifiers) is { } hotkey)
            _viewModel.ApplyShortcut(hotkey);
        // else: only modifiers so far; wait for the actual key
    }

    private void ShortcutButton_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (_viewModel.IsRecordingShortcut)
            _viewModel.StopRecording();
    }

    /// <summary>
    /// Esc closes the window, unless a control used it first: bubbling KeyDown arrives after an open ComboBox closed
    /// its drop-down and after the shortcut button cancelled a recording (both mark the key handled).
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }
}
