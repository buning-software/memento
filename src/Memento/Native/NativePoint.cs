using System.Runtime.InteropServices;

namespace Memento.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct NativePoint
{
    public int X;
    public int Y;
}
