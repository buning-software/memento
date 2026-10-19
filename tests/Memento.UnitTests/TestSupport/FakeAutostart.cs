using System.Security;
using Memento.Settings.Services;

namespace Memento.UnitTests.TestSupport;

internal sealed class FakeAutostart : IAutostart
{
    public bool Enabled { get; set; }

    /// <summary>Simulates a policy that forbids changing the Run key.</summary>
    public bool Blocked { get; set; }

    public bool IsEnabled() => Blocked ? throw new SecurityException("blocked") : Enabled;

    public void SetEnabled(bool enabled)
    {
        if (Blocked)
            throw new UnauthorizedAccessException("blocked");
        Enabled = enabled;
    }
}
