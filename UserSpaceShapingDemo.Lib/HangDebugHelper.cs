using System;
using System.Runtime.CompilerServices;

using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib;

public static class HangDebugHelper
{
    public static Scope Measure(string message, long timeoutNs = 100_000_000) => new(message, timeoutNs);

    public readonly struct Scope : IDisposable
    {
        private readonly long _start = NativeTime.MonotonicNs;
        private readonly string _message;
        private readonly long _timeoutNs;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Scope(string message, long timeoutNs)
        {
            _message = message;
            _timeoutNs = timeoutNs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            var duration = NativeTime.MonotonicNs - _start;
            if (duration > _timeoutNs)
                Console.Error.WriteLine($"[HANG DEBUG] Operation '{_message}' took {duration / 1_000_000} ms");
        }
    }
}