using System;
using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Xpd;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public sealed class ForwardingChannel : IDisposable
{
    private readonly NativeQueue<XdpDescriptor> _packetQueue1 = new();
    private readonly NativeQueue<XdpDescriptor> _packetQueue2 = new();

    public ForwardingMode Mode { get; }

    public UMemory Memory { get; }

    public Pipe Pipe1 { get; }
    public Pipe Pipe2 { get; }

    public NativeQueue<ulong> FreeFrames { get; }

    [SkipLocalsInit]
    public ForwardingChannel(string ifName1, string ifName2, ForwardingMode mode = ForwardingMode.Generic)
    {
        Mode = mode;
        Memory = new UMemory();
        Pipe1 = new Pipe(ifName1, _packetQueue1, _packetQueue2);
        Pipe2 = new Pipe(ifName2, _packetQueue2, _packetQueue1);
        FreeFrames = new NativeQueue<ulong>();

        Span<ulong> frames = stackalloc ulong[(int)Memory.FrameCount];
        Memory.GetAddresses(frames);
        foreach (var frame in frames)
            FreeFrames.Enqueue(frame);
    }

    public void Dispose()
    {
        _packetQueue1.Dispose();
        _packetQueue2.Dispose();
        FreeFrames.Dispose();
    }

    public sealed class Pipe
    {
        public string IfName { get; }
        public NativeQueue<XdpDescriptor> IncomingPackets { get; }
        public NativeQueue<XdpDescriptor> OutgoingPackets { get; }

        internal Pipe(string ifName, NativeQueue<XdpDescriptor> incomingPackets, NativeQueue<XdpDescriptor> outgoingPackets)
        {
            IfName = ifName;
            IncomingPackets = incomingPackets;
            OutgoingPackets = outgoingPackets;
        }
    }
}