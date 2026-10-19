using System.Windows;
using Wpf.Ui.Appearance;
using Memento.Core.Enumerations;
using Memento.Core.Settings.Models;
using Memento.Core.Settings.Services;

namespace Memento.Theme;

/// <summary>
/// Applies the configured theme and re-applies it when the setting changes. "System" follows Windows live
/// (a SystemThemeWatcher per window); Light and Dark are fixed, so a Windows theme change cannot flip them.
/// </summary>
public sealed class ThemeController : IDisposable
{
    private readonly ISettingsStore _settings;
    private readonly List<Window> _windows = [];
    private readonly HashSet<Window> _watched = [];
    private AppTheme? _applied;

    public ThemeController(ISettingsStore settings)
    {
        _settings = settings;
        _settings.Changed += Settings_Changed;
    }

    /// <summary>Makes the window follow the theme setting for as long as it is open.</summary>
    public void Attach(Window window)
    {
        _windows.Add(window);
        window.Closed += (_, _) =>
        {
            _windows.Remove(window);
            _watched.Remove(window);
        };
        UpdateWatcher(window);
    }

    public void Apply()
    {
        var mode = _settings.Current.Theme;
        foreach (var window in _windows)
            UpdateWatcher(window);

        var theme = mode switch
        {
            AppTheme.Dark => ApplicationTheme.Dark,
            AppTheme.Light => ApplicationTheme.Light,
            _ => ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light,
        };
        ApplicationThemeManager.Apply(theme);
        _applied = mode;
    }

    public void Dispose() => _settings.Changed -= Settings_Changed;

    private void Settings_Changed(object? sender, AppSettings settings)
    {
        if (settings.Theme != _applied)
            Apply();
    }

    private void UpdateWatcher(Window window)
    {
        bool follow = _settings.Current.Theme == AppTheme.System;

        if (follow && _watched.Add(window))
        {
            SystemThemeWatcher.Watch(window);
        }
        else if (!follow && _watched.Contains(window))
        {
            // UnWatch throws for a window that has not loaded yet; retry once it has.
            if (window.IsLoaded)
            {
                SystemThemeWatcher.UnWatch(window);
                _watched.Remove(window);
            }
            else
            {
                window.Loaded -= Window_Loaded; // at most one pending retry per window
                window.Loaded += Window_Loaded;
            }
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var window = (Window)sender;
        window.Loaded -= Window_Loaded;
        UpdateWatcher(window);
    }
}
