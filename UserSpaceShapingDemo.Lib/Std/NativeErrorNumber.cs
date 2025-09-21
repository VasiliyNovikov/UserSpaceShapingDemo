using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Std;

[SuppressMessage("Microsoft.Formatting", "IDE0055: Fix formatting", Justification = "Intentional enum value alignment")]
public enum NativeErrorNumber
{
    OK                       = 0,
    OperationNotPermitted    = 1,  // EPERM - Operation not permitted
    NoSuchFileOrDirectory    = 2,  // ENOENT - No such file or directory
    NoSuchProcess            = 3,  // ESRCH - No such process
    InterruptedSystemCall    = 4,  // EINTR - Interrupted system call
    InputOutputError         = 5,  // EIO - I/O error
    NoSuchDeviceOrAddress    = 6,  // ENXIO - No such device or address
    ArgumentListTooLong      = 7,  // E2BIG - Argument list too long
    ExecFormatError          = 8,  // ENOEXEC - Exec format error
    BadFileNumber            = 9,  // EBADF - Bad file number
    NoChildProcesses         = 10, // ECHILD - No child processes
    TryAgain                 = 11, // EAGAIN - Try again
    OutOfMemory              = 12, // ENOMEM - Out of memory
    PermissionDenied         = 13, // EACCES - Permission denied
    BadAddress               = 14, // EFAULT - Bad address
    BlockDeviceRequired      = 15, // ENOTBLK - Block device required
    DeviceOrResourceBusy     = 16, // EBUSY - Device or resource busy
    FileExists               = 17, // EEXIST - File exists
    CrossDeviceLink          = 18, // EXDEV - Cross-device link
    NoSuchDevice             = 19, // ENODEV - No such device
    NotADirectory            = 20, // ENOTDIR - Not a directory
    IsADirectory             = 21, // EISDIR - Is a directory
    InvalidArgument          = 22, // EINVAL - Invalid argument
    FileTableOverflow        = 23, // ENFILE - File table overflow
    TooManyOpenFiles         = 24, // EMFILE - Too many open files
    NotATypewriter           = 25, // ENOTTY - Not a typewriter
    TextFileBusy             = 26, // ETXTBSY - Text file busy
    FileTooLarge             = 27, // EFBIG - File too large
    NoSpaceLeftOnDevice      = 28, // ENOSPC - No space left on device
    IllegalSeek              = 29, // ESPIPE - Illegal seek
    ReadOnlyFileSystem       = 30, // EROFS - Read-only file system
    TooManyLinks             = 31, // EMLINK - Too many links
    BrokenPipe               = 32, // EPIPE - Broken pipe
    MathArgOutOfDomain       = 33, // EDOM - Math argument out of domain of func
    OutOfRange               = 34, // ERANGE - Math result not representable
    ResourceDeadlockAvoided  = 35, // EDEADLK - Resource deadlock would occur
    FileNameTooLong          = 36, // ENAMETOOLONG - File name too long
    NoLocksAvailable         = 37, // ENOLCK - No record locks available
    InvalidSystemCall        = 38, // ENOSYS - Invalid system call number
    DirectoryNotEmpty        = 39, // ENOTEMPTY - Directory not empty
    TooManySymbolicLinks     = 40, // ELOOP - Too many symbolic links encountered
    OperationWouldBlock      = 41, // EWOULDBLOCK - Operation would block
    NoMessageOfDesiredType   = 42, // ENOMSG - No message of desired type
    IdentifierRemoved        = 43, // EIDRM - Identifier removed
    ChannelNumberOutOfRange  = 44, // ECHRNG - Channel number out of range
    Level2NotSynchronized    = 45, // EL2NSYNC - Level 2 not synchronized
    Level3Halted             = 46, // EL3HLT - Level 3 halted
    Level3Reset              = 47, // EL3RST - Level 3 reset
    LinkNumberOutOfRange     = 48, // ELNRNG - Link number out of range
    ProtocolDriverNotAttached= 49, // EUNATCH - Protocol driver not attached
    NoCSIChannel             = 50, // ENOCSI - No CSI structure available
    Level2Halted             = 51, // EL2HLT - Level 2 halted
    InvalidExchange          = 52, // EBADE - Invalid exchange
    InvalidRequestDescriptor = 53, // EBADR - Invalid request descriptor
    ExchangeFull             = 54, // EXFULL - Exchange full
    NoAnode                  = 55, // ENOANO - No anode
    InvalidRequestCode       = 56, // EBADRQC - Invalid request code
    InvalidSlot              = 57, // EBADSLT - Invalid slot
}

public static unsafe class NativeErrorNumberExtensions
{
    private static readonly string?[] ErrorMessageCache = new string[256];

    [ThreadStatic]
    private static NativeErrorNumber* _lastErrorNumberLocation;

    extension(NativeErrorNumber errorNumber)
    {
        public static NativeErrorNumber Last
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_lastErrorNumberLocation is null)
                    _lastErrorNumberLocation = LibC.__errno_location();
                return *_lastErrorNumberLocation;
            }
        }

        public string Message
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int errorNumberInt = (int)errorNumber;
                return (uint)errorNumberInt < ErrorMessageCache.Length
                    ? ErrorMessageCache[errorNumberInt] ??= GetMessage(errorNumber)
                    : GetMessage(errorNumber);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static string GetMessage(NativeErrorNumber errorNumber) => Utf8StringMarshaller.ConvertToManaged(LibC.strerror(errorNumber))!;
            }
        }
    }
}