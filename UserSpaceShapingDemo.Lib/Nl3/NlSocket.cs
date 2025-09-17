using System;
using System.Runtime.ConstrainedExecution;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3;

public sealed unsafe class NlSocket : CriticalFinalizerObject, IDisposable
{
    internal LibNl3.nl_sock* Sock { get; }

    public NlSocket()
    {
        Sock = LibNl3.nl_socket_alloc();
        if (Sock is null)
            throw NlException.FromLastPInvokeError();
    }

    private void ReleaseUnmanagedResources()
    {
        if (Sock is not null)
            LibNl3.nl_socket_free(Sock);
    }

    ~NlSocket() => ReleaseUnmanagedResources();

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public void Connect(NlProtocol protocol)
    {
        var err = LibNl3.nl_connect(Sock, (int)protocol);
        if (err < 0)
            throw new NlException(err);
    }
}