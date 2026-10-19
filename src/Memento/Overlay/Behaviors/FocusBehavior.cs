using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Memento.Overlay.Behaviors;

/// <summary>Focuses a TextBox and selects its text when the bound value turns true (the inline title editor).</summary>
public static class FocusBehavior
{
    public static readonly DependencyProperty IsFocusedProperty = DependencyProperty.RegisterAttached(
        "IsFocused", typeof(bool), typeof(FocusBehavior), new PropertyMetadata(false, OnIsFocusedChanged));

    public static bool GetIsFocused(DependencyObject element) => (bool)element.GetValue(IsFocusedProperty);

    public static void SetIsFocused(DependencyObject element, bool value) => element.SetValue(IsFocusedProperty, value);

    private static void OnIsFocusedChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not System.Windows.Controls.TextBox box || e.NewValue is not true)
            return;

        // The editor becomes visible in the same pass; focusing a collapsed element does nothing, so wait for layout.
        box.Dispatcher.BeginInvoke(() =>
        {
            box.Focus();
            Keyboard.Focus(box);
            box.SelectAll();
        }, DispatcherPriority.Input);
    }
}
