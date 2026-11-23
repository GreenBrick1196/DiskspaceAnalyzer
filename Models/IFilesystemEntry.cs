using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.Models
{
    public interface IFilesystemEntry : INotifyPropertyChanged
    {
        public string IconText { get; }
        public DirectoryData? TopDirectory { get; }
        public DirectoryData? UpperDirectory { get; }
        public ObservableCollection<IFilesystemEntry>? SubNodes { get; }

        public string Path { get; }
        public string Name { get; }
        public string Extension { get; }

        public long LogicalSize { get; }
        public long PhysicalSize { get; }

        public string LogicalSizeText { get; }
        public string PhysicalSizeText { get; }

        public double LogicalSizeDirectoryPercentage { get; }
        public double PhysicalSizeDirectoryPercentage { get; }

        public string LogicalSizeDirectoryPercentageText { get; }
        public string PhysicalSizeDirectoryPercentageText { get; }

        public double LogicalSizeTotalPercentage { get; }
        public double PhysicalSizeTotalPercentage { get; }

        public string LogicalSizeTotalPercentageText { get; }
        public string PhysicalSizeTotalPercentageText { get; }

        public GridLength[] SizeProportionColumnWidths { get; }

        public int DirectoriesInside { get; }
        public int FilesInside { get; }

        public string DirectoriesInsideText { get; }
        public string FilesInsideText { get; }

        public DateTime CreatedTime { get; }
        public DateTime EditedTime { get; }

        public string CreatedTimeText { get; }
        public string EditedTimeText { get; }

        public string FormattedInfo { get; }
    }
}