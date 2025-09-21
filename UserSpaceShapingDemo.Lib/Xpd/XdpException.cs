using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Xpd;

public sealed class XdpException(int error) : NativeException((NativeErrorNumber)(-error));

internal static class XdpExceptionExtensions
{
    public static void ThrowIfError(this LibBpf.xsk_api_result result)
    {
        if (result.error_code < 0)
            throw new XdpException(result.error_code);
    }
}