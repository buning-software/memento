using System.Windows;

namespace Memento.Shell.Services;

internal sealed class MessageBoxDialogService : IDialogService
{
    private const string Caption = "Memento";

    public void ShowError(string message) =>
        MessageBox.Show(message, Caption, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowWarning(string message) =>
        MessageBox.Show(message, Caption, MessageBoxButton.OK, MessageBoxImage.Warning);
}
