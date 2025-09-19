using System;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3;

public abstract unsafe class NlSocket : NativeObject
{
    internal LibNl3.nl_sock* Sock { get; }

    protected NlSocket(NlProtocol protocol)
    {
        try
        {
            Sock = LibNl3.nl_socket_alloc();
            if (Sock is null)
                throw NlException.FromLastPInvokeError();
            try
            {
                LibNl3.nl_connect(Sock, (int)protocol).ThrowIfError();
            }
            catch
            {
                LibNl3.nl_socket_free(Sock);
                throw;
            }
        }
        catch
        {
#pragma warning disable CA1816
            GC.SuppressFinalize(this);
#pragma warning restore CA1816
            throw;
        }
    }

    protected override void ReleaseUnmanagedResources() => LibNl3.nl_socket_free(Sock);
}