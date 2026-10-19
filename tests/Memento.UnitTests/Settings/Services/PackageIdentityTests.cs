using Memento.Settings.Services;

namespace Memento.UnitTests.Settings.Services;

public sealed class PackageIdentityTests
{
    [Fact]
    public void A_process_started_outside_an_msix_is_not_packaged()
    {
        Assert.False(PackageIdentity.IsPackaged);
    }
}
