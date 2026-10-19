using System.ComponentModel;
using System.Diagnostics;

namespace Memento.Settings.Services;

internal sealed class ShellLauncher : IShellLauncher
{
    public void Open(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            // Nothing sensible to do when the shell cannot open it.
        }
    }
}
