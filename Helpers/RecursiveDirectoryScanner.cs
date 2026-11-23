using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using RPZ_DiskspaceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.Helpers
{
    public class RecursiveDirectoryScanner : IDirectoryTreeProvider
    {
        public RecursiveDirectoryScanner()
        {

        }

        private static readonly EnumerationOptions options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint // Skip symbolic links/junctions to avoid loops
        };

        public async Task FillDirectoryTree(DirectoryData topDirectoryData, DirectoryData currentDirectoryData, CancellationToken cts, INotificationManager? notificationManager = null)
        {
            if (cts.IsCancellationRequested)
            {
                return;
            }

            try
            {
                currentDirectoryData.StartInitialization();

                foreach (string subDirectoryPath in Directory.EnumerateDirectories(currentDirectoryData.Path, "*", options))
                {
                    if (!Directory.Exists(subDirectoryPath))
                    {
                        continue;
                    }

                    DirectoryData subDirectoryData = new DirectoryData(topDirectoryData, currentDirectoryData, subDirectoryPath);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        currentDirectoryData.Directories.Add(subDirectoryData);
                    });
                }

                foreach (string filePath in Directory.EnumerateFiles(currentDirectoryData.Path, "*", options))
                {
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    FileData fileData = new FileData(topDirectoryData, currentDirectoryData, filePath);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        currentDirectoryData.Files.Add(fileData);
                    });
                }

                foreach (DirectoryData subDirectoryData in currentDirectoryData.Directories) // make this loop parallel?
                {
                    await FillDirectoryTree(topDirectoryData, subDirectoryData, cts);
                }

                await Dispatcher.UIThread.InvokeAsync(currentDirectoryData.EndInitialization);
            }
            catch (Exception ex)
            {
                if (notificationManager is not null)
                {
                    Notification errorNotification = new Notification()
                    {
                        Type = NotificationType.Error,
                        Title = "Scan error",
                        Message = $"{ex.Message}",
                    };

                    await Dispatcher.UIThread.InvokeAsync(() => notificationManager.Show(errorNotification));
                }
            }
        }
    }
}