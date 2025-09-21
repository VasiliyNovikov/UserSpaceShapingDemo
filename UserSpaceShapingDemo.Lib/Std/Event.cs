using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public sealed class Event(bool isSet, bool blocking) : FileObject(Create(isSet, blocking))
{
    private static FileDescriptor Create(bool isSet, bool blocking)
    {
        int flags = 0;
        if (!blocking)
            flags |= LibC.EFD_NONBLOCK;
        return LibC.eventfd(isSet ? 1u : 0u, flags);
    }
}