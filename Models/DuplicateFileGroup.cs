using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.Models
{
    public class DuplicateFileGroup : ObservableObject, IFilesystemEntry
    {
        public string IconText => "⧉";
        public DirectoryData TopDirectory { get; init; }
        public DirectoryData? UpperDirectory => null;

        public ObservableCollection<IFilesystemEntry> SubNodes { get; init; }

        public string Path { get; init; }
        public string Name { get; init; }
        public string Extension { get; init; }

        public long LogicalSize { get; init; }
        public long PhysicalSize { get; init; }

        public string LogicalSizeText => FileData.FormatFileSize(LogicalSize);
        public string PhysicalSizeText => FileData.FormatFileSize(PhysicalSize);

        public double LogicalSizeDirectoryPercentage => 0;
        public double PhysicalSizeDirectoryPercentage => 0;

        public string LogicalSizeDirectoryPercentageText => string.Empty;
        public string PhysicalSizeDirectoryPercentageText => string.Empty;

        public double LogicalSizeTotalPercentage => (double)LogicalSize * 100 / (TopDirectory.LogicalSize);
        public double PhysicalSizeTotalPercentage => (double)PhysicalSize * 100 / (TopDirectory.PhysicalSize);

        public string LogicalSizeTotalPercentageText => $"{Math.Round(LogicalSizeTotalPercentage, 2)}%";
        public string PhysicalSizeTotalPercentageText => $"{Math.Round(PhysicalSizeTotalPercentage, 2)}%";

        public GridLength[] SizeProportionColumnWidths => [];

        public int DirectoriesInside => 0;
        public int FilesInside => SubNodes.Count;

        public string DirectoriesInsideText => string.Empty;
        public string FilesInsideText => SubNodes.Count.ToString();

        public DateTime CreatedTime => DateTime.MinValue;
        public DateTime EditedTime => DateTime.MinValue;

        public string CreatedTimeText => string.Empty;
        public string EditedTimeText => string.Empty;

        public string Fingerprint => (SubNodes?[0] as FileData)?.Fingerprint ?? "NULL";
        public string HashMD5 => (SubNodes?[0] as FileData)?.HashMD5 ?? "NULL";

        public string FormattedInfo => $"DuplicateFileGroup {(SubNodes[0] as FileData)!.HashMD5}" +
            $"\nTotal LogicalSize = {FileData.FormatFileSize(LogicalSize)}" +
            $"\nTotal PhysicalSize = {FileData.FormatFileSize(PhysicalSize)}" +
            $"\nConsists of {FilesInside} duplicates";

        public DuplicateFileGroup(DirectoryData topDirectory, List<FileData> files)
        {
            TopDirectory = topDirectory;
            SubNodes = [.. files];

            Path = string.Empty;
            Name = $"Duplicate Group {HashMD5}";
            Extension = string.Empty;

            LogicalSize = SubNodes.Sum(node => node.LogicalSize);
            PhysicalSize = SubNodes.Sum(node => node.PhysicalSize);
        }
    }
}