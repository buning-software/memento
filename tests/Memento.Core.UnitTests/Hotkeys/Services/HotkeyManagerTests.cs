using Memento.Core.Enumerations;
using Memento.Core.Hotkeys.Models;
using Memento.Core.Hotkeys.Services;
using Memento.Core.Settings.Models;
using Memento.Core.UnitTests.TestSupport;

namespace Memento.Core.UnitTests.Hotkeys.Services;

public class HotkeyManagerTests
{
    private static readonly Hotkey CtrlAltM = new(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x4D);
    private static readonly Hotkey CtrlAltN = new(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x4E);

    private readonly FakeHotkeyRegistrar _registrar = new();
    private readonly InMemorySettingsStore _settings = new(new AppSettings().WithHotkey(CtrlAltM));
    private readonly HotkeyManager _manager;
    private int _presses;

    public HotkeyManagerTests() => _manager = new HotkeyManager(_registrar, _settings);

    private HotkeyStartupResult Start() => _manager.RegisterConfigured(() => _presses++);

    [Fact]
    public void A_free_configured_hotkey_is_registered_and_calls_the_handler()
    {
        var result = Start();

        Assert.Equal(new HotkeyStartupResult(HotkeyStartupOutcome.Registered, CtrlAltM), result);
        _registrar.Registered[CtrlAltM]();
        Assert.Equal(1, _presses);
    }

    [Fact]
    public void A_taken_configured_hotkey_falls_back_to_the_default_and_saves_it()
    {
        _registrar.TakenByOtherApps.Add(CtrlAltM);

        var result = Start();

        Assert.Equal(new HotkeyStartupResult(HotkeyStartupOutcome.FellBackToDefault, CtrlAltM), result);
        Assert.Equal(Hotkey.Default, Assert.Single(_registrar.Registered).Key);
        Assert.Equal(Hotkey.Default, _settings.Current.Hotkey);
    }

    [Fact]
    public void A_taken_default_hotkey_is_reported_without_changing_settings()
    {
        _settings.Update(s => s.WithHotkey(Hotkey.Default));
        _registrar.TakenByOtherApps.Add(Hotkey.Default);

        var result = Start();

        Assert.Equal(HotkeyStartupOutcome.DefaultInUse, result.Outcome);
        Assert.Empty(_registrar.Registered);
    }

    [Fact]
    public void When_configured_and_default_are_both_taken_nothing_is_registered()
    {
        _registrar.TakenByOtherApps.Add(CtrlAltM);
        _registrar.TakenByOtherApps.Add(Hotkey.Default);

        var result = Start();

        Assert.Equal(HotkeyStartupOutcome.ConfiguredAndDefaultInUse, result.Outcome);
        Assert.Empty(_registrar.Registered);
        Assert.Equal(CtrlAltM, _settings.Current.Hotkey);
    }

    [Fact]
    public void TryChange_to_a_free_hotkey_replaces_the_old_one_and_saves_it()
    {
        Start();

        Assert.True(_manager.TryChange(CtrlAltN));

        Assert.Equal(CtrlAltN, Assert.Single(_registrar.Registered).Key);
        Assert.Equal(CtrlAltN, _settings.Current.Hotkey);
        _registrar.Registered[CtrlAltN]();
        Assert.Equal(1, _presses);
    }

    [Fact]
    public void TryChange_to_a_taken_hotkey_keeps_the_previous_one_working()
    {
        Start();
        _registrar.TakenByOtherApps.Add(CtrlAltN);

        Assert.False(_manager.TryChange(CtrlAltN));

        Assert.Equal(CtrlAltM, Assert.Single(_registrar.Registered).Key);
        Assert.Equal(CtrlAltM, _settings.Current.Hotkey);
    }

    [Fact]
    public void When_the_previous_hotkey_cannot_be_restored_a_later_change_back_registers_it_again()
    {
        Start();
        _registrar.TakenByOtherApps.Add(CtrlAltN);
        _registrar.TakenByOtherApps.Add(CtrlAltM); // grabbed by another app while it was briefly unregistered

        Assert.False(_manager.TryChange(CtrlAltN));
        Assert.Empty(_registrar.Registered);

        _registrar.TakenByOtherApps.Clear();
        Assert.True(_manager.TryChange(CtrlAltM));

        Assert.Equal(CtrlAltM, Assert.Single(_registrar.Registered).Key);
    }

    [Fact]
    public void TryChange_to_the_active_hotkey_succeeds_without_registering_again()
    {
        Start();
        int updatesBefore = _settings.UpdateCount;

        Assert.True(_manager.TryChange(CtrlAltM));

        Assert.Single(_registrar.Registered);
        Assert.Equal(updatesBefore, _settings.UpdateCount);
    }

    [Fact]
    public void A_free_configured_hotkey_leaves_the_settings_untouched()
    {
        Start();

        Assert.Equal(0, _settings.UpdateCount);
    }

    [Fact]
    public void The_default_hotkey_is_registered_when_it_is_the_configured_one()
    {
        _settings.Update(s => s.WithHotkey(Hotkey.Default));
        int updatesBefore = _settings.UpdateCount;

        var result = Start();

        Assert.Equal(new HotkeyStartupResult(HotkeyStartupOutcome.Registered, Hotkey.Default), result);
        Assert.Equal(Hotkey.Default, Assert.Single(_registrar.Registered).Key);
        Assert.Equal(updatesBefore, _settings.UpdateCount);
    }

    [Fact]
    public void The_fallback_saves_the_default_exactly_once_and_its_handler_works()
    {
        _registrar.TakenByOtherApps.Add(CtrlAltM);

        Start();

        Assert.Equal(1, _settings.UpdateCount);
        _registrar.Registered[Hotkey.Default]();
        Assert.Equal(1, _presses);
    }

    [Fact]
    public void A_taken_default_leaves_the_settings_untouched()
    {
        _settings.Update(s => s.WithHotkey(Hotkey.Default));
        int updatesBefore = _settings.UpdateCount;
        _registrar.TakenByOtherApps.Add(Hotkey.Default);

        Start();

        Assert.Equal(updatesBefore, _settings.UpdateCount);
        Assert.Equal(Hotkey.Default, _settings.Current.Hotkey);
    }

    [Fact]
    public void A_failed_fallback_leaves_the_settings_untouched()
    {
        _registrar.TakenByOtherApps.Add(CtrlAltM);
        _registrar.TakenByOtherApps.Add(Hotkey.Default);

        Start();

        Assert.Equal(0, _settings.UpdateCount);
    }

    [Fact]
    public void A_failed_TryChange_does_not_touch_the_settings_and_the_previous_handler_still_works()
    {
        Start();
        _registrar.TakenByOtherApps.Add(CtrlAltN);

        Assert.False(_manager.TryChange(CtrlAltN));

        Assert.Equal(0, _settings.UpdateCount);
        _registrar.Registered[CtrlAltM]();
        Assert.Equal(1, _presses);
    }

    [Fact]
    public void A_failed_TryChange_can_be_retried_once_the_hotkey_is_free()
    {
        Start();
        _registrar.TakenByOtherApps.Add(CtrlAltN);
        Assert.False(_manager.TryChange(CtrlAltN));
        _registrar.TakenByOtherApps.Remove(CtrlAltN);

        Assert.True(_manager.TryChange(CtrlAltN));

        Assert.Equal(CtrlAltN, Assert.Single(_registrar.Registered).Key);
        Assert.Equal(CtrlAltN, _settings.Current.Hotkey);
        Assert.Equal(1, _settings.UpdateCount);
    }

    [Fact]
    public void TryChange_twice_in_a_row_leaves_only_the_last_hotkey_registered()
    {
        Start();

        Assert.True(_manager.TryChange(CtrlAltN));
        Assert.True(_manager.TryChange(Hotkey.Default));

        Assert.Equal(Hotkey.Default, Assert.Single(_registrar.Registered).Key);
        Assert.Equal(Hotkey.Default, _settings.Current.Hotkey);
        Assert.Equal(2, _settings.UpdateCount);
    }

    [Fact]
    public void TryChange_before_the_start_registers_a_handler_that_does_nothing()
    {
        Assert.True(_manager.TryChange(CtrlAltN));

        _registrar.Registered[CtrlAltN]();
        Assert.Equal(0, _presses);
        Assert.Equal(CtrlAltN, _settings.Current.Hotkey);
    }

    [Fact]
    public void TryChange_after_a_failed_start_can_register_the_configured_hotkey()
    {
        _registrar.TakenByOtherApps.Add(CtrlAltM);
        _registrar.TakenByOtherApps.Add(Hotkey.Default);
        Start();
        _registrar.TakenByOtherApps.Clear();

        Assert.True(_manager.TryChange(CtrlAltM));

        Assert.Equal(CtrlAltM, Assert.Single(_registrar.Registered).Key);
    }
}
