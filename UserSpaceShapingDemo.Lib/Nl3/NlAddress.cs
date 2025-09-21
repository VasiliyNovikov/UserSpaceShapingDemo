using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3;

public sealed unsafe class NlAddress : NativeObject
{
    private readonly bool _owned = true;
    private const int MaxAddressStringLength = 128;

    internal LibNl3.nl_addr* Addr { get; }

    public byte Length => (byte)LibNl3.nl_addr_get_len(Addr);

    public byte PrefixLength
    {
        get => (byte)LibNl3.nl_addr_get_prefixlen(Addr);
        set => LibNl3.nl_addr_set_prefixlen(Addr, value);
    }

    public AddressFamily Family
    {
        get => (AddressFamily)LibNl3.nl_addr_get_family(Addr);
        set => LibNl3.nl_addr_set_family(Addr, (int)value);
    }

    public ReadOnlySpan<byte> Bytes
    {
        get
        {
            var len = Length;
            if (len == 0)
                return default;

            var ptr = LibNl3.nl_addr_get_binary_addr(Addr);
            return ptr is null ? default : new(ptr, len);
        }
    }

    internal NlAddress(LibNl3.nl_addr* addr, bool owned)
    {
        _owned = owned;
        ArgumentNullException.ThrowIfNull(addr);
        Addr = addr;
    }

    [SkipLocalsInit]
    public NlAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        Span<byte> addressBytes = stackalloc byte[16];
        address.TryWriteBytes(addressBytes, out int length);
        Addr = Build(addressBytes[..length], address.AddressFamily);
    }

    public NlAddress(ReadOnlySpan<byte> addressBytes, AddressFamily family)
    {
        Addr = Build(addressBytes, family);
    }

    private static LibNl3.nl_addr* Build(ReadOnlySpan<byte> addressBytes, AddressFamily family)
    {
        fixed (byte* buf = addressBytes)
        {
            var addr = LibNl3.nl_addr_build((int)family, buf, (nuint)addressBytes.Length);
            return addr is null ? throw NlException.FromLastNativeError() : addr;
        }
    }

    protected override void ReleaseUnmanagedResources()
    {
        if (Addr is not null && _owned)
            LibNl3.nl_addr_put(Addr);
    }

    [SkipLocalsInit]
    public override string ToString()
    {
        var addressBuffer = stackalloc byte[MaxAddressStringLength];
        var str = LibNl3.nl_addr2str(Addr, addressBuffer, MaxAddressStringLength);
        return str is null
            ? throw NlException.FromLastNativeError()
            : Utf8StringMarshaller.ConvertToManaged(str)!;
    }

    public static NlAddress Parse(string address)
    {
        ArgumentNullException.ThrowIfNull(address);
        LibNl3.nl_addr_parse(address, 0, out var addr).ThrowIfError();
        return new(addr, true);
    }
}