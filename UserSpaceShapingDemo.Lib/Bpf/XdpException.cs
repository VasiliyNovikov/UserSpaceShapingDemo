using System.ComponentModel;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Bpf;

public sealed class XdpException(int error) : Win32Exception(-error);

internal static class XdpExceptionExtensions
{
    public static void ThrowIfError(this LibBpf.xsk_api_result result)
    {
        if (result.Error < 0)
            throw new XdpException(result.Error);
    }
}