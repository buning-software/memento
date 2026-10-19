namespace Memento.Settings.Services;

public interface IAutostart
{
    bool IsEnabled();
    void SetEnabled(bool enabled);
}