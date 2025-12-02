using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3;

internal readonly unsafe struct NlObject(LibNl3.nl_object* obj)
{
    public LibNl3.nl_object* Obj => obj;
}