using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public static unsafe class Poll
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Wait(Span<Query> queries, int timeoutMilliseconds)
    {
        fixed (Query* queriesPtr = queries)
        {
            var result = LibC.poll((LibC.pollfd*)queriesPtr, (uint)queries.Length, timeoutMilliseconds);
            if (!result.IsError())
                return result > 0;

            var error = NativeErrorNumber.Last;
            return error == NativeErrorNumber.InterruptedSystemCall
                ? false
                : throw new NativeException(error);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Wait(Span<Query> queries, TimeSpan timeout) => Wait(queries, (int)timeout.TotalMilliseconds);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Event? Wait(FileDescriptor descriptor, Event @event, int timeoutMilliseconds)
    {
        Span<Query> queries = [new(descriptor, @event)];
        return Wait(queries, timeoutMilliseconds)
            ? queries[0].ReturnedEvents
            : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Event? Wait(FileDescriptor descriptor, Event @event, TimeSpan timeout) => Wait(descriptor, @event, (int)timeout.TotalMilliseconds);

    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("Style", "IDE0032: Use auto property", Justification = "Struct layout")]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly struct Query(FileDescriptor descriptor, Event events)
    {
        private readonly FileDescriptor _descriptor = descriptor;
        private readonly Event _events = events;
        private readonly Event _returnedEvents;

        public FileDescriptor Descriptor => _descriptor;
        public Event Events => _events;
        public Event ReturnedEvents => _returnedEvents;
    }

    [Flags]
    public enum Event : short
    {
        None = 0,
        Readable = LibC.POLLIN,
        Urgent   = LibC.POLLPRI,
        Writable = LibC.POLLOUT,
        Error    = LibC.POLLERR,
        HangUp   = LibC.POLLHUP,
        Invalid  = LibC.POLLNVAL
    }
}