using System;
using System.Runtime.ConstrainedExecution;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3;

public unsafe class NlSocket : CriticalFinalizerObject, IDisposable
{
    internal LibNl3.nl_sock* Sock { get; }

    public NlSocket(NlProtocol protocol)
    {
        Sock = LibNl3.nl_socket_alloc();
        if (Sock is null)
            throw NlException.FromLastPInvokeError();
        var err = LibNl3.nl_connect(Sock, (int)protocol);
        if (err < 0)
        {
            LibNl3.nl_socket_free(Sock);
            Sock = null;
            throw new NlException(err);
        }
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
}