using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.Models
{
    public partial class DirectoryData : ObservableObject, IFilesystemEntry
    {
        public string IconText => Path.EndsWith(':') ? "🖴" : "📁";
        public DirectoryData? TopDirectory { get; init; }
        public DirectoryData? UpperDirectory { get; init; }

        private bool _startedInitialization = false;
        private bool _finishedInitialization = false;

        public bool StartedInitialization
        {
            get => _startedInitialization;
            private set => SetProperty(ref _startedInitialization, value);
        }
        public bool FinishedInitialization
        {
            get => _finishedInitialization;
            private set => SetProperty(ref _finishedInitialization, value);
        }

        public string Path { get; init; }
        public string Name => Path.Contains('\\') ? System.IO.Path.GetFileName(Path) : Path; // despite saying "FileName" it works for getting directory names too
        public string Extension => string.Empty;

        public ObservableCollection<DirectoryData> Directories { get; init; }
        public ObservableCollection<FileData> Files { get; init; }
        public ObservableCollection<IFilesystemEntry> SubNodes => new ObservableCollection<IFilesystemEntry>(Directories.Concat<IFilesystemEntry>(Files ?? []));

        private long _logicalSize;
        private long _physicalSize;

        public long LogicalSize
        {
            get => _logicalSize;
            private set
            {
                if (SetProperty(ref _logicalSize, value))
                {
                    OnPropertyChanged(nameof(LogicalSizeText));
                    OnPropertyChanged(nameof(LogicalSizeDirectoryPercentage));
                    OnPropertyChanged(nameof(LogicalSizeDirectoryPercentageText));
                    OnPropertyChanged(nameof(LogicalSizeTotalPercentage));
                    OnPropertyChanged(nameof(LogicalSizeTotalPercentageText));
                }
            }
        }
        public long PhysicalSize
        {
            get => _physicalSize;
            private set
            {
                if (SetProperty(ref _physicalSize, value))
                {
                    OnPropertyChanged(nameof(PhysicalSizeText));
                    OnPropertyChanged(nameof(PhysicalSizeDirectoryPercentage));
                    OnPropertyChanged(nameof(PhysicalSizeDirectoryPercentageText));
                    OnPropertyChanged(nameof(PhysicalSizeTotalPercentage));
                    OnPropertyChanged(nameof(PhysicalSizeTotalPercentageText));
                    OnPropertyChanged(nameof(SizeProportionColumnWidths));
                }
            }
        }

        public string LogicalSizeText => FileData.FormatFileSize(LogicalSize) + (!FinishedInitialization ? " and counting" : string.Empty);
        public string PhysicalSizeText => FileData.FormatFileSize(PhysicalSize) + (!FinishedInitialization ? " and counting" : string.Empty);

        public double LogicalSizeDirectoryPercentage => (double)LogicalSize * 100 / (UpperDirectory?.LogicalSize ?? LogicalSize);
        public double PhysicalSizeDirectoryPercentage => (double)PhysicalSize * 100 / (UpperDirectory?.PhysicalSize ?? PhysicalSize);

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

        private int _directoriesInside;
        private int _filesInside;

        public int DirectoriesInside
        {
            get => _directoriesInside;
            private set
            {
                if (SetProperty(ref _directoriesInside, value))
                {
                    OnPropertyChanged(nameof(DirectoriesInsideText));
                }
            }
        }
        public int FilesInside
        {
            get => _filesInside;
            private set
            {
                if (SetProperty(ref _filesInside, value))
                {
                    OnPropertyChanged(nameof(FilesInsideText));
                }
            }
        }

        public string DirectoriesInsideText => DirectoriesInside + (!FinishedInitialization ? " and counting" : string.Empty);
        public string FilesInsideText => FilesInside + (!FinishedInitialization ? " and counting" : string.Empty);

        public DateTime CreatedTime { get; init; }
        public DateTime EditedTime { get; init; }

        public string CreatedTimeText => CreatedTime.ToString("G");
        public string EditedTimeText => EditedTime.ToString("G");

        public string FormattedInfo => $"Directory {Path}" +
            $"\nLogicalSize = {FileData.FormatFileSize(LogicalSize)}" +
            $"\nPhysicalSize = {FileData.FormatFileSize(PhysicalSize)}" +
            $"\nTakes {LogicalSizeDirectoryPercentageText} of directory logic space, {LogicalSizeTotalPercentageText} total" +
            $"\nTakes {PhysicalSizeDirectoryPercentageText} of directory physical space, {PhysicalSizeTotalPercentageText} total" +
            $"\nHas {DirectoriesInside} ({Directories.Count}) subdirectories" +
            $"\nHas {FilesInside} ({Files.Count}) files" +
            $"\nCreated {CreatedTimeText}" +
            $"\nEdited {EditedTimeText}";

        public DirectoryData(DirectoryData? topDirectory, DirectoryData? upperDirectory, string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new ArgumentException($"Directory {directoryPath} not found");
            }

            TopDirectory = topDirectory;
            UpperDirectory = upperDirectory;

            if (directoryPath.EndsWith('\\'))
            {
                directoryPath = directoryPath.Substring(0, directoryPath.Length - 1);
            }
            Path = directoryPath;

            DirectoryInfo directoryInfo = new DirectoryInfo(Path);
            CreatedTime = directoryInfo.CreationTime;
            EditedTime = directoryInfo.LastWriteTime;

            Files = new ObservableCollection<FileData>();
            Files.CollectionChanged += Files_CollectionChanged;

            Directories = new ObservableCollection<DirectoryData>();
            Directories.CollectionChanged += Directories_CollectionChanged;
        }

        private void Files_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            long logicalSizeChange = 0;
            long physicalSizeChange = 0;
            int filesInsideChange = 0;

            if (e.NewItems is not null)
            {
                foreach (FileData item in e.NewItems)
                {
                    filesInsideChange = filesInsideChange + 1;

                    logicalSizeChange = logicalSizeChange + item.LogicalSize;
                    physicalSizeChange = physicalSizeChange + item.PhysicalSize;
                }
            }

            if (e.OldItems is not null)
            {
                foreach (FileData item in e.OldItems)
                {
                    filesInsideChange = filesInsideChange - 1;

                    logicalSizeChange = logicalSizeChange - item.LogicalSize;
                    physicalSizeChange = physicalSizeChange - item.PhysicalSize;
                }
            }

            OnPropertyChanged(nameof(SubNodes));
            SendFieldUpdatesUpDirectoryTree(logicalSizeChange, physicalSizeChange, 0, filesInsideChange);
        }

        private void Directories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            long logicalSizeChange = 0;
            long physicalSizeChange = 0;
            int directoriesInsideChange = 0;
            int filesInsideChange = 0;

            if (e.NewItems is not null)
            {
                foreach (DirectoryData item in e.NewItems)
                {
                    directoriesInsideChange = directoriesInsideChange + 1;

                    logicalSizeChange = logicalSizeChange + item.LogicalSize;
                    physicalSizeChange = physicalSizeChange + item.PhysicalSize;
                    directoriesInsideChange = directoriesInsideChange + item.DirectoriesInside;
                    filesInsideChange = filesInsideChange + item.FilesInside;
                }
            }

            if (e.OldItems is not null)
            {
                foreach (DirectoryData item in e.OldItems)
                {
                    directoriesInsideChange = directoriesInsideChange - 1;

                    logicalSizeChange = logicalSizeChange - item.LogicalSize;
                    physicalSizeChange = physicalSizeChange - item.PhysicalSize;
                    directoriesInsideChange = directoriesInsideChange - item.DirectoriesInside;
                    filesInsideChange = filesInsideChange - item.FilesInside;
                }
            }

            OnPropertyChanged(nameof(SubNodes));
            SendFieldUpdatesUpDirectoryTree(logicalSizeChange, physicalSizeChange, directoriesInsideChange, filesInsideChange);
        }


        private void SendFieldUpdatesUpDirectoryTree(long logicalSizeChange, long physicalSizeChange, int directoriesInsideChange, int filesInsideChange)
        {
            LogicalSize = LogicalSize + logicalSizeChange;
            PhysicalSize = PhysicalSize + physicalSizeChange;
            DirectoriesInside = DirectoriesInside + directoriesInsideChange;
            FilesInside = FilesInside + filesInsideChange;

            if (UpperDirectory is not null)
            {
                UpperDirectory.SendFieldUpdatesUpDirectoryTree(logicalSizeChange, physicalSizeChange, directoriesInsideChange, filesInsideChange);
            }
        }

        private static void RecalculateSizePercentages(DirectoryData directoryData)
        {
            directoryData.OnPropertyChanged(nameof(LogicalSizeDirectoryPercentage));
            directoryData.OnPropertyChanged(nameof(LogicalSizeDirectoryPercentageText));
            directoryData.OnPropertyChanged(nameof(LogicalSizeTotalPercentage));
            directoryData.OnPropertyChanged(nameof(LogicalSizeTotalPercentageText));

            directoryData.OnPropertyChanged(nameof(PhysicalSizeDirectoryPercentage));
            directoryData.OnPropertyChanged(nameof(PhysicalSizeDirectoryPercentageText));
            directoryData.OnPropertyChanged(nameof(PhysicalSizeTotalPercentage));
            directoryData.OnPropertyChanged(nameof(PhysicalSizeTotalPercentageText));

            directoryData.OnPropertyChanged(nameof(SizeProportionColumnWidths));

            foreach (FileData fileData in directoryData.Files)
            {
                fileData.RecalculateSizePercentages();
            }

            foreach (DirectoryData subDirectoryData in directoryData.Directories)
            {
                RecalculateSizePercentages(subDirectoryData);
            }
        }

        public void StartInitialization()
        {
            StartedInitialization = true;
        }

        public void EndInitialization()
        {
            FinishedInitialization = true;
            OnPropertyChanged(nameof(LogicalSizeText));
            OnPropertyChanged(nameof(PhysicalSizeText));
            OnPropertyChanged(nameof(DirectoriesInsideText));
            OnPropertyChanged(nameof(FilesInsideText));
            RecalculateSizePercentages(TopDirectory ?? this);
        }

        public void RollBackInitializationEndUpDirectoryTree()
        {
            FinishedInitialization = false;

            if (UpperDirectory is not null)
            {
                UpperDirectory.RollBackInitializationEndUpDirectoryTree();
            }
        }

        public void PropagateInitializationEndUpDirectoryTree()
        {
            EndInitialization();

            if (UpperDirectory is not null)
            {
                UpperDirectory.PropagateInitializationEndUpDirectoryTree();
            }
        }

        public DirectoryDataDTO ToDTO()
        {
            return new DirectoryDataDTO
            {
                Path = Path,

                LogicalSize = LogicalSize,
                PhysicalSize = PhysicalSize,

                DirectoriesInside = DirectoriesInside,
                FilesInside = FilesInside,

                StartedInitialization = StartedInitialization,
                FinishedInitialization = FinishedInitialization,

                CreatedTime = CreatedTime,
                EditedTime = EditedTime,

                Directories = Directories.Select(directoryData => directoryData.ToDTO()).ToList(),
                Files = Files.Select(fileData => fileData.ToDTO()).ToList(),
            };
        }

        public DirectoryData(DirectoryData? topDirectory, DirectoryData? upperDirectory, DirectoryDataDTO directoryDataDTO) // FromDTO
        {
            StartedInitialization = directoryDataDTO.StartedInitialization;

            TopDirectory = topDirectory;
            UpperDirectory = upperDirectory;

            Path = directoryDataDTO.Path;

            CreatedTime = directoryDataDTO.CreatedTime;
            EditedTime = directoryDataDTO.EditedTime;

            Files = new ObservableCollection<FileData>();
            Files.CollectionChanged += Files_CollectionChanged;

            Directories = new ObservableCollection<DirectoryData>();

            foreach (FileDataDTO fileDataDTO in directoryDataDTO.Files)
            {
                Files.Add(new FileData(topDirectory ?? this, this, fileDataDTO));
            }

            foreach (DirectoryDataDTO subdirectoryDataDTO in directoryDataDTO.Directories)
            {
                Directories.Add(new DirectoryData(topDirectory ?? this, this, subdirectoryDataDTO));
            }
            SendFieldUpdatesUpDirectoryTree(0, 0, Directories.Count, 0);
            Directories.CollectionChanged += Directories_CollectionChanged; // must be after adding directories because if it is placed before then the size of directories
                                                                            // gets counted multiple times, exploding total size (also affects file and dir inside count)
            FinishedInitialization = directoryDataDTO.FinishedInitialization;
        }

        public async Task ToJson(Stream outputStream)
        {
            DirectoryDataDTO dto = ToDTO();

            await JsonSerializer.SerializeAsync(outputStream, dto, new JsonSerializerOptions
            {
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            });
        }

        public static async Task<DirectoryData?> FromJson(Stream inputStream)
        {
            DirectoryDataDTO? dto = await JsonSerializer.DeserializeAsync<DirectoryDataDTO>(inputStream);
            if (dto is null)
            {
                return null;
            }

            return new DirectoryData(null, null, dto);
        }
    }
}
