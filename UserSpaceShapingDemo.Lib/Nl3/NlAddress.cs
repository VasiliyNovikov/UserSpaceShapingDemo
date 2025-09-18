using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3;

public sealed unsafe class NlAddress : CriticalFinalizerObject, IDisposable
{
    private const int MaxAddressStringLength = 128;

    internal LibNl3.nl_addr* Addr { get; }

    internal NlAddress(LibNl3.nl_addr* addr)
    {
        Addr = addr == null ? throw new ArgumentNullException(nameof(addr)) : addr;
    }

    [SkipLocalsInit]
    public NlAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        Span<byte> addressBytes = stackalloc byte[16];
        address.TryWriteBytes(addressBytes, out int size);
        fixed (byte* p = addressBytes)
        {
            var addr = LibNl3.nl_addr_build((int)address.AddressFamily, p, (nuint)size);
            if (addr == null)
                throw NlException.FromLastPInvokeError();
            Addr = addr;
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (Addr is not null)
            LibNl3.nl_addr_put(Addr);
    }

    ~NlAddress() => ReleaseUnmanagedResources();

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    [SkipLocalsInit]
    public override string ToString()
    {
        var addressBuffer = stackalloc byte[MaxAddressStringLength];
        var str = LibNl3.nl_addr2str(Addr, addressBuffer, MaxAddressStringLength);
        return str is null
            ? throw NlException.FromLastPInvokeError()
            : Utf8StringMarshaller.ConvertToManaged(str)!;
    }

    public static NlAddress Parse(string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        var error = LibNl3.nl_addr_parse(address, 0, out var addr);
        return error < 0
            ? throw new NlException(error)
            : new(addr);
    }
}