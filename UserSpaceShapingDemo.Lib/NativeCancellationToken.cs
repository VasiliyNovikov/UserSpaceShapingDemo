using System;
using System.Runtime.CompilerServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib;

public sealed class NativeCancellationToken : NativeObject
{
    public static NativeCancellationToken None => new(CancellationToken.None);

    private readonly CancellationToken _cancellationToken;
    private readonly NativeEvent? _event;
    private readonly CancellationTokenRegistration? _cancellationRegistration;

    public NativeCancellationToken(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        if (cancellationToken.CanBeCanceled)
        {
            _event = new NativeEvent(false);
            _cancellationRegistration = cancellationToken.Register(() => _event.Set());
        }
    }

    protected override void ReleaseUnmanagedResources()
    {
        _cancellationRegistration?.Dispose();
        _event?.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Wait(FileDescriptor descriptor, Poll.Event events)
    {
        if (_event is null)
            Poll.Wait(descriptor, events, -1);
        else
        {
            Span<Poll.Query> queries = [new(_event.Descriptor, Poll.Event.Readable), new(descriptor, events)];
            Poll.Wait(queries, -1);
            if ((queries[0].ReturnedEvents & Poll.Event.Readable) == Poll.Event.Readable)
                _cancellationToken.ThrowIfCancellationRequested();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WaitRead(FileDescriptor descriptor) => Wait(descriptor, Poll.Event.Readable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WaitWrite(FileDescriptor descriptor) => Wait(descriptor, Poll.Event.Writable);
}