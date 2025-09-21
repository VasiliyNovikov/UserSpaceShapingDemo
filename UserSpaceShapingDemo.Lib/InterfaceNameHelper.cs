using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib;

public static class InterfaceNameHelper
{
    public static int GetIndex(string name)
    {
        var index = LibC.if_nametoindex(name);
        return index == 0 ? throw NativeException.FromLastError() : (int)index;
    }
}