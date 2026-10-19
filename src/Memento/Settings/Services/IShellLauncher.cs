namespace Memento.Settings.Services;

/// <summary>Opens a folder, file or URI (such as ms-settings:colors) the way Explorer would.</summary>
public interface IShellLauncher
{
    void Open(string target);
}
