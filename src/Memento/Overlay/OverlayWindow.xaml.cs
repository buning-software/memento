using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using Memento.Core.Enumerations;
using Memento.Core.Settings.Services;
using Memento.Enumerations;
using Memento.Native;
using Memento.Overlay.Behaviors;
using Memento.Overlay.ViewModels;
using Memento.Theme;

namespace Memento.Overlay;

/// <summary>
/// The floating capture overlay. Created once and prewarmed (handle, Mica, first layout); after that it is
/// only shown and hidden, which stays well under 20 ms. All behaviour lives in <see cref="OverlayViewModel"/>;
/// this class maps WPF input to it, follows its focus, and positions the window.
/// </summary>
public partial class OverlayWindow : FluentWindow
{
    /// <summary>Distance from the top of the screen, as a fraction of the work area height.</summary>
    private const double TopOffsetRatio = 0.18;

    private readonly OverlayViewModel _viewModel;
    private readonly ISettingsStore _settings;

    public OverlayWindow(OverlayViewModel viewModel, ISettingsStore settings, ThemeController theme)
    {
        _viewModel = viewModel;
        _settings = settings;
        DataContext = viewModel;
        InitializeComponent();
        theme.Attach(this);

        viewModel.CloseRequested += (_, _) => HideOverlay();
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    /// <summary>
    /// Creates the HWND, the Mica backdrop and the first layout off-screen, so the first hotkey press does not pay
    /// for a cold start. Call once, after the database is ready (it loads the list).
    /// </summary>
    public void Prewarm()
    {
        _viewModel.Refresh();

        double originalOpacity = Opacity;
        Opacity = 0;
        ShowActivated = false;
        Left = -10_000;
        Top = -10_000;

        Show();
        UpdateLayout();
        Hide();

        ShowActivated = true;
        Opacity = originalOpacity == 0 ? 1 : originalOpacity;
    }

    public void ShowOverlay()
    {
        if (IsVisible)
        {
            Activate();
            FocusInput();
            return;
        }

        PositionOverlay();
        _viewModel.OnShown();
        Show();
        Activate();
        FocusInput();

        // Refresh after the first render (Render runs before Loaded), so loading the list never slows down the
        // pop-up, but before any input (Loaded runs before Input), so a quick Down or F2 is not reset by it.
        Dispatcher.BeginInvoke(_viewModel.Refresh, DispatcherPriority.Loaded);
    }

    public void HideOverlay()
    {
        _viewModel.CancelEdit(); // an unfinished edit is dropped
        if (IsVisible)
            Hide();
    }

    public void ToggleOverlay()
    {
        if (IsVisible && IsActive)
            HideOverlay();
        else
            ShowOverlay();
    }

    /// <summary>Centres the overlay horizontally near the top of the configured monitor (primary, or the one under the mouse).</summary>
    private void PositionOverlay()
    {
        var screen = _settings.Current.Monitor == OverlayMonitor.Cursor
            ? System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position)
            : System.Windows.Forms.Screen.PrimaryScreen ?? System.Windows.Forms.Screen.AllScreens[0];
        var area = screen.WorkingArea; // physical pixels (the app is per-monitor DPI aware)

        double monitorScale = NativeMethods.GetMonitorScale(area.Left + area.Width / 2, area.Top + area.Height / 2);
        double windowScale = VisualTreeHelper.GetDpi(this).DpiScaleX;

        double leftPx = area.Left + (area.Width - Width * monitorScale) / 2;
        double topPx = area.Top + area.Height * TopOffsetRatio;

        // Window.Left/Top are in DIPs of the window's current DPI.
        Left = leftPx / windowScale;
        Top = topPx / windowScale;
    }

    private void FocusInput()
    {
        // The hotkey grants the right to take the foreground: claim it, then give WPF focus to the input.
        NativeMethods.SetForegroundWindow(new WindowInteropHelper(this).Handle);
        InputBox.Focus();
        Keyboard.Focus(InputBox);
        InputBox.SelectAll();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OverlayViewModel.Focus))
            return;

        switch (_viewModel.Focus)
        {
            case OverlayFocus.Input:
                Keyboard.Focus(InputBox);
                break;
            case OverlayFocus.Settings:
                Keyboard.Focus(SettingsButton);
                break;
            // OverlayFocus.Editor: FocusBehavior focuses the row's editor once it is visible.
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (ToOverlayKey(e.Key) is { } key && _viewModel.HandleKey(key))
            e.Handled = true;
    }

    private static OverlayKey? ToOverlayKey(Key key) => key switch
    {
        Key.Enter => OverlayKey.Enter,
        Key.Space => OverlayKey.Space,
        Key.Escape => OverlayKey.Escape,
        Key.Up => OverlayKey.Up,
        Key.Down => OverlayKey.Down,
        Key.F2 => OverlayKey.F2,
        // Ctrl+Tab and Alt+Tab are not list navigation.
        Key.Tab when (Keyboard.Modifiers & ~ModifierKeys.Shift) == ModifierKeys.None =>
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? OverlayKey.ShiftTab : OverlayKey.Tab,
        _ => null,
    };

    private void TaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TaskList.SelectedItem is { } item)
            TaskList.ScrollIntoView(item);
    }

    private void TitleEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        _viewModel.CancelEdit(); // clicking elsewhere cancels the edit

    private void SettingsButton_FocusChanged(object sender, KeyboardFocusChangedEventArgs e) =>
        _viewModel.SettingsFocusChanged(SettingsButton.IsKeyboardFocused);

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => _viewModel.RequestSettings();

    /// <summary>Losing focus hides the overlay, except to its own settings window, which stays open on top of it like a dialog.</summary>
    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (OwnedWindows.Count == 0)
            HideOverlay();
    }

    /// <summary>Back from the settings window: the overlay is as freshly opened (no selection, input focused).</summary>
    public void ResetAfterSettings()
    {
        if (!IsVisible)
            return;

        _viewModel.OnShown();
        Activate();
        FocusInput();
    }
}
