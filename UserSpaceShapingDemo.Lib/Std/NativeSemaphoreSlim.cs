using System;
using System.Runtime.CompilerServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public sealed class NativeSemaphoreSlim(uint initialValue = 0)
    : NativeEventBase(initialValue == 0 ? 0u : 1u, LibC.EFD_SEMAPHORE)
{
    private ulong _count = initialValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Increment()
    {
        if (Interlocked.Increment(ref _count) == 1)
            WriteOne();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(uint value)
    {
        if (value > 0 && Interlocked.Add(ref _count, value) == value)
            WriteOne();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryDecrement()
    {
        ulong count;
        while ((count = Interlocked.Read(ref _count)) > 0)
        {
            if (Interlocked.CompareExchange(ref _count, count - 1, count) == count)
            {
                if (count == 1)
                    Read();
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint TryRemove(uint value)
    {
        if (value == 0)
            return 0;
        ulong count;
        while ((count = Interlocked.Read(ref _count)) > 0)
        {
            var toRemove = (uint)Math.Min(count, value);
            if (Interlocked.CompareExchange(ref _count, count - toRemove, count) == count)
            {
                if (count == toRemove)
                    Read();
                return toRemove;
            }
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Decrement()
    {
        while (!TryDecrement())
            Poll.Wait(Descriptor, Poll.Event.Readable, Timeout.Infinite);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(uint value)
    {
        var remaining = value;
        while ((remaining -= TryRemove(remaining)) > 0)
            Poll.Wait(Descriptor, Poll.Event.Readable, Timeout.Infinite);
    }
}