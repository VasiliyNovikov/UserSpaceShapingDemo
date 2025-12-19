using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UserSpaceShapingDemo.Lib.Headers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct IPv6ExtensionHeader : IHasNextHeader
{
    private IPProtocol _nextHeader;
    private byte _extensionLength;
    private fixed byte _options[6];

    public IPProtocol Protocol
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _nextHeader;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _nextHeader = value;
    }

    public byte HeaderLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (byte)((_extensionLength + 1) * 8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _extensionLength = (byte)((value / 8) - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T NextHeader<T>() where T : unmanaged
    {
        return ref Unsafe.As<byte, T>(ref Unsafe.Add(ref Unsafe.As<IPv6ExtensionHeader, byte>(ref Unsafe.AsRef(in this)), HeaderLength));
    }
}