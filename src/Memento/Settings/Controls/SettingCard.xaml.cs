using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Memento.Settings.Controls;

/// <summary>
/// One setting: icon, header and description on the left, the setting's control (the content) on the right.
/// <see cref="Details"/> goes under the description, for extras such as an error line or a link.
/// </summary>
public partial class SettingCard : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(SymbolRegular), typeof(SettingCard), new PropertyMetadata(SymbolRegular.Empty));

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingCard), new PropertyMetadata(""));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingCard), new PropertyMetadata(null));

    public static readonly DependencyProperty DetailsProperty =
        DependencyProperty.Register(nameof(Details), typeof(object), typeof(SettingCard), new PropertyMetadata(null));

    public SettingCard() => InitializeComponent();

    public SymbolRegular Icon
    {
        get => (SymbolRegular)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Optional: no description line when null.</summary>
    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Optional content under the description.</summary>
    public object? Details
    {
        get => GetValue(DetailsProperty);
        set => SetValue(DetailsProperty, value);
    }
}
