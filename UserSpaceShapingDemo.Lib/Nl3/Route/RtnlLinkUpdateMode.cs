using System;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

[Flags]
public enum RtnlLinkUpdateMode
{
    None = 0,
    Replace = LibNlRoute3.NLM_F_REPLACE,
    Exclusive = LibNlRoute3.NLM_F_EXCL,
    Create = LibNlRoute3.NLM_F_CREATE,
    Append = LibNlRoute3.NLM_F_APPEND
}