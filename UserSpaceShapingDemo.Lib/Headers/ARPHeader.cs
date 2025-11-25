using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ARPHeader
{
    private NetInt<ushort> _hardwareType;
    private NetInt<ushort> _protocolType;
    public byte HardwareAddressLength;
    public byte ProtocolAddressLength;
    private NetInt<ushort> _operation;
    public MACAddress SenderHardwareAddress;
    public IPv4Address SenderProtocolAddress;
    public MACAddress TargetHardwareAddress;
    public IPv4Address TargetProtocolAddress;

    public ushort HardwareType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ushort)_hardwareType;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _hardwareType = (NetInt<ushort>)value;
    }

    public EthernetType ProtocolType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (EthernetType)(ushort)_protocolType;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _protocolType = (NetInt<ushort>)(ushort)value;
    }

    public ARPOperation Operation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (ARPOperation)(ushort)_operation;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _operation = (NetInt<ushort>)(ushort)value;
    }
}