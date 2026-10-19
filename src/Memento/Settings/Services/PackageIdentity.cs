using Memento.Native;

namespace Memento.Settings.Services;

/// <summary>Whether Memento runs from an installed MSIX package, which changes how "Start with Windows" has to work.</summary>
internal static class PackageIdentity
{
    public static bool IsPackaged { get; } = DetectPackaged();

    private static bool DetectPackaged()
    {
        uint length = 0;
        return NativeMethods.GetCurrentPackageFullName(ref length, IntPtr.Zero) != NativeMethods.APPMODEL_ERROR_NO_PACKAGE;
    }
}
