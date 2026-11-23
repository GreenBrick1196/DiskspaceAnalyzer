using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.Helpers
{
    public static class FilesystemHelper
    {
        public static void Open(string path)
        {
            ValidatePath(path);

            Process.Start(new ProcessStartInfo
            {
                FileName = Path.GetFullPath(path),
                UseShellExecute = true
            });
        }

        public static void SelectInFileManager(string path)
        {
            ValidatePath(path);

            path = Path.GetFullPath(path);

            if (OperatingSystem.IsWindows())
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    UseShellExecute = false,
                    Arguments = $"/select,\"{path}\"" // Використав Arguments замість ArgumentList тому що ArgumentList додає " на початку і в кінці кожного аргементу, а 
                };                                    // explorer.exe не розпізнає /select якщо перед ним йде "

                Process.Start(startInfo);
            }
            else if (OperatingSystem.IsMacOS()) // Not tested
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "open",
                    UseShellExecute = false
                };

                startInfo.ArgumentList.Add("-R");
                startInfo.ArgumentList.Add(path);

                Process.Start(startInfo);
            }
            else if (OperatingSystem.IsLinux())
            {
                OpenInLinuxFileManager(path);
            }
            else
            {
                throw new PlatformNotSupportedException("File manager integration is not supported on this platform.");
            }
        }

        private static void OpenInLinuxFileManager(string path)
        {
            // Nautilus (GNOME)
            var nautilus = new ProcessStartInfo
            {
                FileName = "nautilus",
                UseShellExecute = false
            };

            nautilus.ArgumentList.Add("--select");
            nautilus.ArgumentList.Add(path);

            try
            {
                Process.Start(nautilus);
                return;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Nautilus is not installed. Try Dolphin.
            }

            // Dolphin (KDE)
            var dolphin = new ProcessStartInfo
            {
                FileName = "dolphin",
                UseShellExecute = false
            };

            dolphin.ArgumentList.Add("--select");
            dolphin.ArgumentList.Add(path);

            try
            {
                Process.Start(dolphin);
                return;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // No supported file manager found.
            }

            // Generic fallback: open the containing directory.
            var directory = Directory.Exists(path)
                ? Directory.GetParent(path)?.FullName ?? path
                : Path.GetDirectoryName(path)!;

            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                ArgumentList = { directory },
                UseShellExecute = false
            });
        }

        private static void ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be empty.", nameof(path));
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new FileNotFoundException("Path does not exist.", path);
            }
        }
    }
}
