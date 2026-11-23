using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.Helpers
{
    public static class FilePhysicalSizeHelper
    {
        public static long? GetFileAllocatedSize(string filePath)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    long clusterSize = GetWindowsClusterSize(filePath) ?? -1;
                    if (clusterSize == -1)
                    {
                        return null;
                    }

                    return GetWindowsFileAllocatedSize(filePath, clusterSize);
                }
                if (OperatingSystem.IsLinux())
                {
                    return GetLinuxFileAllocatedSize(filePath);
                }
                if (OperatingSystem.IsMacOS())
                {
                    return GetMacFileAllocatedSize(filePath);
                }
            }
            catch { }

            return null;
        }


        private static long GetWindowsFileAllocatedSize(string filePath, long clusterSize)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            long length = fileInfo.Length;

            if (length == 0)
            {
                return 0;
            }

            bool isCompressedOrSparse = (fileInfo.Attributes & FileAttributes.Compressed) != 0 || (fileInfo.Attributes & FileAttributes.SparseFile) != 0;
            if (!isCompressedOrSparse)
            {
                return ((length + clusterSize - 1) / clusterSize) * clusterSize;
            }

            string pathForWin = filePath.StartsWith(@"\\?\", StringComparison.Ordinal) ? filePath : @"\\?\" + filePath;

            using SafeFileHandle handle = WindowsNative.CreateFile(
                pathForWin,
                WindowsNative.GENERIC_READ,
                WindowsNative.FILE_SHARE_READ | WindowsNative.FILE_SHARE_WRITE | WindowsNative.FILE_SHARE_DELETE,
                IntPtr.Zero,
                WindowsNative.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (!handle.IsInvalid &&
                WindowsNative.GetFileInformationByHandleEx(
                    handle,
                    WindowsNative.FileStandardInfo,
                    out WindowsNative.FILE_STANDARD_INFO info,
                    (uint)Marshal.SizeOf<WindowsNative.FILE_STANDARD_INFO>()))
            {
                return info.AllocationSize;
            }

            return length;
        }

        private static long? GetWindowsClusterSize(string path)
        {
            string? root = Path.GetPathRoot(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(root) && WindowsNative.GetDiskFreeSpace(root, out uint sectorsPerCluster, out uint bytesPerSector, out _, out _))
            {
                long size = (long)sectorsPerCluster * bytesPerSector;
                return size > 0 ? size : null;
            }

            return null;
        }

        private static long? GetLinuxFileAllocatedSize(string filePath)
        {
            if (LinuxNative.lstat(filePath, out LinuxNative.StatLinux buf) == 0)
            {
                return buf.st_blocks * 512L;
            }

            return null;
        }

        private static long? GetMacFileAllocatedSize(string filePath)
        {
            if (MacNative.lstat64(filePath, out MacNative.StatMac64 buf) == 0)
            {
                return buf.st_blocks * 512L;
            }

            return null;
        }

        private static class WindowsNative
        {
            public const int FileStandardInfo = 1;
            public const uint GENERIC_READ = 0x80000000;
            public const uint FILE_SHARE_READ = 0x00000001;
            public const uint FILE_SHARE_WRITE = 0x00000002;
            public const uint FILE_SHARE_DELETE = 0x00000004;
            public const uint OPEN_EXISTING = 3;

            [StructLayout(LayoutKind.Sequential)]
            public struct FILE_STANDARD_INFO
            {
                public long AllocationSize;
                public long EndOfFile;
                public uint NumberOfLinks;
                [MarshalAs(UnmanagedType.Bool)] 
                public bool DeletePending;
                [MarshalAs(UnmanagedType.Bool)]
                public bool Directory;
            }

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern SafeFileHandle CreateFile(
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetFileInformationByHandleEx(
                SafeFileHandle hFile,
                int fileInformationClass,
                out FILE_STANDARD_INFO lpFileInformation,
                uint dwBufferSize);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetDiskFreeSpace(
                string lpRootPathName,
                out uint lpSectorsPerCluster,
                out uint lpBytesPerSector,
                out uint lpNumberOfClusters,
                out uint lpTotalNumberOfClusters);
        }

        private static class LinuxNative
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct StatLinux
            {
                public ulong st_dev;
                public ulong st_ino;
                public ulong st_nlink;
                public uint st_mode;
                public uint st_uid;
                public uint st_gid;
                public int __pad0;
                public ulong st_rdev;
                public long st_size;
                public long st_blksize;
                public long st_blocks;
                public long st_atime;
                public long st_atime_nsec;
                public long st_mtime;
                public long st_mtime_nsec;
                public long st_ctime;
                public long st_ctime_nsec;
                private long __unused0;
                private long __unused1;
                private long __unused2;
            }

            [DllImport("libc", SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern int lstat(string path, out StatLinux buf);
        }

        private static class MacNative
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct StatMac64
            {
                public int st_dev;
                public ushort st_mode;
                public ushort st_nlink;
                public ulong st_ino;
                public uint st_uid;
                public uint st_gid;
                public int st_rdev;
                public long st_atime;
                public long st_atime_nsec;
                public long st_mtime;
                public long st_mtime_nsec;
                public long st_ctime;
                public long st_ctime_nsec;
                public long st_birthtime;
                public long st_birthtime_nsec;
                public long st_size;
                public long st_blocks;
                public int st_blksize;
                public uint st_flags;
                public uint st_gen;
                public int st_lspare;
                public long st_qspare0;
                public long st_qspare1;
            }

            [DllImport("libc", EntryPoint = "lstat64", SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern int lstat64(string path, out StatMac64 buf);
        }
    }
}
