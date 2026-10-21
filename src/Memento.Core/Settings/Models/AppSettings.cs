using System.Text.Json.Serialization;
using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;
using Memento.Core.Settings.Converters;
using Memento.Core.Settings.Services;

namespace Memento.Core.Settings.Models;

/// <summary>
/// User settings, saved as settings.json. Immutable: change them with <c>with</c> through <see cref="ISettingsStore.Update"/>.
/// </summary>
public sealed record AppSettings
{
    /// <summary>Stored as a number (Win32 MOD_* flags).</summary>
    [JsonConverter(typeof(JsonNumberEnumConverter<HotkeyModifiers>))]
    public HotkeyModifiers HotkeyModifiers { get; init; } = Hotkey.Default.Modifiers;

    /// <summary>Windows virtual-key code.</summary>
    public uint HotkeyKey { get; init; } = Hotkey.Default.VirtualKey;

    public AppTheme Theme { get; init; } = AppTheme.Dark;

    /// <summary>Time of day for a deadline with a date but no time ("tomorrow", "friday"). Stored as "HH:mm".</summary>
    [JsonConverter(typeof(HourMinuteJsonConverter))]
    public TimeOnly DefaultTime { get; init; } = new(9, 0);

    public OverlayMonitor Monitor { get; init; } = OverlayMonitor.Primary;

    /// <summary>Whether a Windows notification is shown when a task's deadline is near.</summary>
    public bool RemindersEnabled { get; init; } = true;

    /// <summary>How long before the deadline the notification is shown.</summary>
    public int ReminderLeadMinutes { get; init; } = 15;

    [JsonIgnore]
    public Hotkey Hotkey => new(HotkeyModifiers, HotkeyKey);

    public AppSettings WithHotkey(Hotkey hotkey) =>
        this with { HotkeyModifiers = hotkey.Modifiers, HotkeyKey = hotkey.VirtualKey };
}
