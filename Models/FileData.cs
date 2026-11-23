using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using RPZ_DiskspaceAnalyzer.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace RPZ_DiskspaceAnalyzer.Models
{
    public partial class FileData : ObservableObject, IFilesystemEntry
    {
        public string IconText => "📄";
        public DirectoryData TopDirectory { get; init; }
        public DirectoryData UpperDirectory { get; init; }
        public ObservableCollection<IFilesystemEntry>? SubNodes => null;

        public string Path { get; init; }
        public string Name => System.IO.Path.GetFileName(Path);
        public string Extension => System.IO.Path.GetExtension(Path);

        public long LogicalSize { get; init; }
        public long PhysicalSize { get; init; }

        public string LogicalSizeText => FormatFileSize(LogicalSize);
        public string PhysicalSizeText => FormatFileSize(PhysicalSize);

        public double LogicalSizeDirectoryPercentage => (double)LogicalSize * 100 / UpperDirectory.LogicalSize;
        public double PhysicalSizeDirectoryPercentage => (double)PhysicalSize * 100 / UpperDirectory.PhysicalSize;

        public string LogicalSizeDirectoryPercentageText => $"{Math.Round(LogicalSizeDirectoryPercentage, 2)}%";
        public string PhysicalSizeDirectoryPercentageText => $"{Math.Round(PhysicalSizeDirectoryPercentage, 2)}%";

        public double LogicalSizeTotalPercentage => (double)LogicalSize * 100 / (TopDirectory?.LogicalSize ?? LogicalSize);
        public double PhysicalSizeTotalPercentage => (double)PhysicalSize * 100 / (TopDirectory?.PhysicalSize ?? PhysicalSize);

        public string LogicalSizeTotalPercentageText => $"{Math.Round(LogicalSizeTotalPercentage, 2)}%";
        public string PhysicalSizeTotalPercentageText => $"{Math.Round(PhysicalSizeTotalPercentage, 2)}%";

        public GridLength[] SizeProportionColumnWidths
        { 
            get
            {
                if (PhysicalSizeDirectoryPercentage is double.NaN || PhysicalSizeTotalPercentage is double.NaN)
                {
                    return [new GridLength(0, GridUnitType.Star), new GridLength(0, GridUnitType.Star)];
                }
                double widthTotal = PhysicalSizeTotalPercentage;

                GridLength col1Width = new GridLength(widthTotal, GridUnitType.Star);    
                GridLength col2Width = new GridLength(Math.Max(100 - widthTotal, 0), GridUnitType.Star);    

                return [col1Width, col2Width];
            }
        }

        public void RecalculateSizePercentages()
        {
            OnPropertyChanged(nameof(LogicalSizeDirectoryPercentage));
            OnPropertyChanged(nameof(PhysicalSizeDirectoryPercentage));
            OnPropertyChanged(nameof(LogicalSizeDirectoryPercentageText));
            OnPropertyChanged(nameof(PhysicalSizeDirectoryPercentageText));

            OnPropertyChanged(nameof(LogicalSizeTotalPercentage));
            OnPropertyChanged(nameof(PhysicalSizeTotalPercentage));
            OnPropertyChanged(nameof(LogicalSizeTotalPercentageText));
            OnPropertyChanged(nameof(PhysicalSizeTotalPercentageText));

            OnPropertyChanged(nameof(SizeProportionColumnWidths));
        }

        public int DirectoriesInside => 0;
        public int FilesInside => 0;

        public string DirectoriesInsideText => string.Empty;
        public string FilesInsideText => string.Empty;

        public DateTime CreatedTime { get; init; }
        public DateTime EditedTime { get; init; }

        public string CreatedTimeText => CreatedTime.ToString("G");
        public string EditedTimeText => EditedTime.ToString("G");

        private string fingerprint = string.Empty;
        public string Fingerprint
        {
            get
            {
                if (fingerprint == string.Empty)
                {
                    fingerprint = ComputeFingerprint();
                }
                return fingerprint;
            }
        }

        private string hashMD5 = string.Empty;
        public string HashMD5
        {
            get
            {
                if (hashMD5 == string.Empty)
                {
                    hashMD5 = ComputeHashMD5();
                }
                return hashMD5;
            }
        }

        public string FormattedInfo => $"File {Path}" +
            $"\nLogicalSize = {FormatFileSize(LogicalSize)}" +
            $"\nPhysicalSize = {FormatFileSize(PhysicalSize)}" +
            $"\nTakes {LogicalSizeDirectoryPercentageText} of directory logic space, {LogicalSizeTotalPercentageText} total" +
            $"\nTakes {PhysicalSizeDirectoryPercentageText} of directory physical space, {PhysicalSizeTotalPercentageText} total" +
            $"\nCreated {CreatedTimeText}" +
            $"\nEdited {EditedTimeText}";

        public FileData(DirectoryData topDirectory, DirectoryData directory, string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"File {filePath} not found");
            }

            TopDirectory = topDirectory;
            UpperDirectory = directory;

            Path = filePath;

            FileInfo fileInfo = new FileInfo(Path);
            LogicalSize = fileInfo.Length;
            PhysicalSize = FilePhysicalSizeHelper.GetFileAllocatedSize(Path) ?? LogicalSize;
            CreatedTime = fileInfo.CreationTime;
            EditedTime = fileInfo.LastWriteTime;
        }

        private string ComputeFingerprint()
        {
            const int CHUNK_SIZE = 4096;

            if (LogicalSize < CHUNK_SIZE * 4)
            {
                return HashMD5;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using var stream = File.OpenRead(Path);

                byte[] buffer = new byte[CHUNK_SIZE];
                long fileSize = stream.Length;

                ReadChunk(stream, ms, buffer, 0, CHUNK_SIZE);
                ReadChunk(stream, ms, buffer, Math.Max(0, (fileSize - CHUNK_SIZE) / 2), CHUNK_SIZE);
                ReadChunk(stream, ms, buffer, Math.Max(0, fileSize - CHUNK_SIZE), CHUNK_SIZE);

                byte[] hash = MD5.HashData(ms);
                return Convert.ToHexString(hash);
            }

            static void ReadChunk(FileStream fileStream, MemoryStream memoryStream, byte[] buffer, long position, int chunkSize)
            {
                fileStream.Position = position;
                int bytesRead = fileStream.Read(buffer, 0, (int)Math.Min(chunkSize, fileStream.Length - position));
                memoryStream.Write(buffer, 0, bytesRead);
            }
        }

        private string ComputeHashMD5()
        {
            using (FileStream fs = File.OpenRead(Path))
            {
                byte[] hash = MD5.HashData(fs);
                return Convert.ToHexString(hash);
            }
        }

        public FileDataDTO ToDTO()
        {
            return new FileDataDTO
            {
                Path = Path,

                LogicalSize = LogicalSize,
                PhysicalSize = PhysicalSize,

                CreatedTime = CreatedTime,
                EditedTime = EditedTime,

                Fingerprint = fingerprint, // using field instead of the property because property would start computation if it wasn't done before and nuke performance
                HashMD5 = hashMD5,         // using field instead of the property because property would start computation if it wasn't done before and nuke performance
            };
        }

        public FileData(DirectoryData topDirectory, DirectoryData directory, FileDataDTO fileDataDTO) // FromDTO
        {
            TopDirectory = topDirectory;
            UpperDirectory = directory;

            Path = fileDataDTO.Path;

            LogicalSize = fileDataDTO.LogicalSize;
            PhysicalSize = fileDataDTO.PhysicalSize;
            CreatedTime = fileDataDTO.CreatedTime;
            EditedTime = fileDataDTO.EditedTime;

            fingerprint = fileDataDTO.Fingerprint;
            hashMD5 = fileDataDTO.HashMD5;
        }

        public static string FormatFileSize(double fileSizeBytes)
        {
            string[] units = ["Bytes", "KB", "MB", "GB", "TB", "PB", "EB"];

            if (fileSizeBytes == 0)
            {
                return "0 Bytes";
            }

            int index = (int)Math.Floor(Math.Log(fileSizeBytes, 1024));
            double size = fileSizeBytes / Math.Pow(1024, index);

            return $"{Math.Round(size, 2)} {units[index]}";
        }
    }
}