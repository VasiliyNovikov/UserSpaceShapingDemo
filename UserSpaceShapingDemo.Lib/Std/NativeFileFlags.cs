using System;
using System.Diagnostics.CodeAnalysis;

namespace UserSpaceShapingDemo.Lib.Std;

[Flags]
[SuppressMessage("Microsoft.Formatting", "IDE0055: Fix formatting", Justification = "Intentional enum value alignment")]
public enum NativeFileFlags
{
    ReadOnly     = 0x000000, // O_RDONLY: Open for reading only
    WriteOnly    = 0x000001, // O_WRONLY: Open for writing only
    ReadWrite    = 0x000002, // O_RDWR: Open for reading and writing
    Create       = 0x000040, // O_CREAT: Create file if it does not exist
    Exclusive    = 0x000080, // O_EXCL: Error if O_CREAT and the file exists
    NoCTTY       = 0x000100, // O_NOCTTY: Do not assign controlling terminal from file
    Truncate     = 0x000200, // O_TRUNC: Truncate file to zero length
    Append       = 0x000400, // O_APPEND: Move file pointer to the end of the file for each write
    NonBlock     = 0x000800, // O_NONBLOCK: Do not block on open or for data to become available
    DataSync     = 0x001000, // O_DSYNC: Write according to synchronized I/O data integrity completion
    Async        = 0x002000, // O_ASYNC: Enable signal-driven I/O
    Direct       = 0x004000, // O_DIRECT: Minimize cache effects of the I/O to and from this file
    LargeFile    = 0x008000, // O_LARGEFILE: Allow large file support
    Directory    = 0x010000, // O_DIRECTORY: Must be a directory
    NoFollow     = 0x020000, // O_NOFOLLOW: Do not follow symlinks
    NoAccessTime = 0x040000, // O_NOATIME: Do not update the file last access time on read
    CloseOnExec  = 0x080000, // O_CLOEXEC: Set close-on-exec flag for the new file descriptor
    Path         = 0x100000, // O_PATH: Obtain a file descriptor that can be used for two purposes
    TmpFile      = 0x210000  // O_TMPFILE: Create an unnamed temporary file
}