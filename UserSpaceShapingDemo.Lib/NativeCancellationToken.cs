using System;
using System.Runtime.CompilerServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib;

public sealed class NativeCancellationToken : NativeObject
{
    private const Poll.Event NativePollEvent = Poll.Event.Readable;
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
    public IFileObject? Wait(ReadOnlySpan<IFileObject> objects, Poll.Event events)
    {
        var objectCount = objects.Length;
        Span<Poll.Query> queries = stackalloc Poll.Query[objectCount + 1];
        for (var i = 0; i < objectCount; ++i)
            queries[i] = new(objects[i].Descriptor, events);
        if (_event is null)
            queries = queries[..objectCount];
        else
            queries[objectCount] = new(_event.Descriptor, NativePollEvent);

        if (Poll.Wait(queries, -1))
        {
            for (var i = 0; i < objectCount; ++i)
                if (queries[i].ReturnedEvents != default)
                    return objects[i];

            if (_event is not null && (queries[objectCount].ReturnedEvents & NativePollEvent) == NativePollEvent)
                _cancellationToken.ThrowIfCancellationRequested();
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Wait(IFileObject @object, Poll.Event events) => Wait([@object], events) is not null;
}