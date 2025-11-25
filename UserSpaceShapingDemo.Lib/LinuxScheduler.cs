using System;

using UserSpaceShapingDemo.Lib.Interop;
using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib;

public static class LinuxScheduler
{
    public enum Policy
    {
        Other = LibC.SCHED_OTHER,
        Fifo = LibC.SCHED_FIFO,
        RoundRobin = LibC.SCHED_RR,
        Batch = LibC.SCHED_BATCH,
        Idle = LibC.SCHED_IDLE,
        Deadline = LibC.SCHED_DEADLINE
    }

    public static void SetScheduler(int pid, Policy policy, int priority)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pid);
        if (priority is < 0 or > 99)
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be between 0 and 99.");
        LibC.sched_setscheduler(pid, (int)policy, new LibC.sched_param { sched_priority = priority }).ThrowIfError();
    }

    public static void SetScheduler(Policy policy, int priority) => SetScheduler(Environment.ProcessId, policy, priority);
}