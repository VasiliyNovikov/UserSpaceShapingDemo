using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class XdpForwarder : IDisposable
{
    private readonly XdpSocket _socket;
    private readonly NativeQueue<ulong> _freeFrames;
    private readonly NativeQueue<XdpDescriptor> _incomingPackets;
    private readonly NativeQueue<XdpDescriptor> _outgoingPackets;

    public XdpForwarder(ForwardingChannel channel, ForwardingChannel.Pipe pipe, bool shared = false)
    {
        _socket = new XdpSocket(channel.Memory, pipe.Eth, shared: shared);
        _freeFrames = channel.FreeFrames;
        _incomingPackets = pipe.IncomingPackets;
        _outgoingPackets = pipe.OutgoingPackets;
    }
    public void Dispose() => _socket.Dispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FillOnce()
    {
        throw new NotImplementedException();
    }
}