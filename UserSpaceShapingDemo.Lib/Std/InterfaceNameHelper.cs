using LinuxCore;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public static class InterfaceNameHelper
{
    public static int GetIndex(string name)
    {
        var index = LibC.if_nametoindex(name);
        return index == 0 ? throw LinuxException.FromLastError() : (int)index;
    }
}