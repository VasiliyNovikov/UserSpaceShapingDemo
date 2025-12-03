using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3.Route;

public enum RtnlBridgePortState : byte
{
    Disabled = LibNlRoute3.BR_STATE_DISABLED,
    Listening = LibNlRoute3.BR_STATE_LISTENING,
    Learning = LibNlRoute3.BR_STATE_LEARNING,
    Forwarding = LibNlRoute3.BR_STATE_FORWARDING,
    Blocking = LibNlRoute3.BR_STATE_BLOCKING
}