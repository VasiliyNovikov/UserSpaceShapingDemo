using System.Runtime.CompilerServices;
using System.Threading;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public sealed class NativeSemaphoreSlim(uint initialValue = 0)
    : NativeEventBase(initialValue == 0 ? 0u : 1u, LibC.EFD_SEMAPHORE)
{
    private long _count = initialValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Increment()
    {
        if (Interlocked.Increment(ref _count) == 1)
            WriteOne();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryDecrement()
    {
        long count;
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
    public void Decrement()
    {
        while (!TryDecrement())
            Poll.Wait(Descriptor, Poll.Event.Readable, Timeout.Infinite);
    }
}