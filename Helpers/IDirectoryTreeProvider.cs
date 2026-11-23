using Avalonia.Controls.Notifications;
using RPZ_DiskspaceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace RPZ_DiskspaceAnalyzer.Helpers
{
    public interface IDirectoryTreeProvider
    {
        public Task FillDirectoryTree(DirectoryData topDirectoryData, DirectoryData currentDirectoryData, CancellationToken cts, INotificationManager? notificationManager = null);
    }
}
