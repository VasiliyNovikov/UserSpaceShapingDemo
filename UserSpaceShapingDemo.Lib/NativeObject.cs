using System;
using System.Runtime.ConstrainedExecution;

namespace UserSpaceShapingDemo.Lib;

public abstract class NativeObject : CriticalFinalizerObject, IDisposable
{
    protected abstract void ReleaseUnmanagedResources();

    ~NativeObject() => ReleaseUnmanagedResources();

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}