using System;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

[Flags]
public enum RtnlAddressFlags : uint
{
    Secondary = LibNlRoute3.IFA_F_SECONDARY,
    NoDAD = LibNlRoute3.IFA_F_NODAD,
    Optimistic = LibNlRoute3.IFA_F_OPTIMISTIC,
    DadFailed = LibNlRoute3.IFA_F_DADFAILED,
    HomeAddress = LibNlRoute3.IFA_F_HOMEADDRESS,
    Deprecated = LibNlRoute3.IFA_F_DEPRECATED,
    Tentative = LibNlRoute3.IFA_F_TENTATIVE,
    Permanent = LibNlRoute3.IFA_F_PERMANENT,
    ManageTempAddr = LibNlRoute3.IFA_F_MANAGETEMPADDR,
    NoPrefixRoute = LibNlRoute3.IFA_F_NOPREFIXROUTE
}