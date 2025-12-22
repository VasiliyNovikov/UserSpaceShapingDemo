using System.Net.Sockets;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

public sealed class NativeSocket(NativeAddressFamily addressFamily, SocketType type, ProtocolType protocol)
    : FileObject(LibC.socket(addressFamily, type, protocol).ThrowIfError());