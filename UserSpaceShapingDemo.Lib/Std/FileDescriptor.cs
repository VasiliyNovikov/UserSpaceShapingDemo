using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Std;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FileDescriptor
{
    private readonly int _fd;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => _fd.ToString(CultureInfo.InvariantCulture);
}