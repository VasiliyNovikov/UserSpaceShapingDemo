using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EthernetHeader
{
    public MACAddress DestinationAddress;
    public MACAddress SourceAddress;
    private NetInt<ushort> _etherType;

    public EthernetType EtherType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (EthernetType)(ushort)_etherType;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _etherType = (NetInt<ushort>)(ushort)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T NextHeader<T>() where T : unmanaged => ref Unsafe.As<EthernetHeader, T>(ref Unsafe.Add(ref Unsafe.AsRef(ref this), 1));
}