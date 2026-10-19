namespace Memento.Shell.Services;

/// <summary>Modal messages, behind an interface so view models stay testable.</summary>
public interface IDialogService
{
    void ShowError(string message);

    void ShowWarning(string message);
}
