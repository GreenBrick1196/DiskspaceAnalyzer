using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RPZ_DiskspaceAnalyzer.Helpers;
using RPZ_DiskspaceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.ViewModels
{
    public partial class DuplicateFilesViewModel : ViewModelBase
    {
        private readonly INotificationManager _notificationManager;

        [ObservableProperty]
        private DirectoryData _topDirectoryData;

        [ObservableProperty]
        private ObservableCollection<DuplicateFileGroup> _duplicateGroups;

        public bool AreThereAnyDuplicateGroups => DuplicateGroups.Count > 0;

        [ObservableProperty]
        private ExtensionFilterViewModel _extensionFilterViewModel;

        [ObservableProperty]
        private HierarchicalTreeDataGridSource<IFilesystemEntry> _treeDataGridSource;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsItemSelected))]
        [NotifyPropertyChangedFor(nameof(FilesystemEntryInfoDisplayViewModel))]
        private IFilesystemEntry? _selectedItem;

        public bool IsItemSelected => SelectedItem is not null;

        public FilesystemEntryInfoDisplayViewModel? FilesystemEntryInfoDisplayViewModel => SelectedItem is not null ? new FilesystemEntryInfoDisplayViewModel(SelectedItem) : null;


        [ObservableProperty]
        private bool _initializationFinished = false;

        private int _initializationStage = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressBarText))]
        [NotifyPropertyChangedFor(nameof(IsProgressBarIndeterminate))]
        private string _initializationStageDescription = string.Empty;

        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public string ProgressBarText { 
            get 
            {
                if (_initializationStage == 0)
                {
                    return "Waiting until directory tree scanning is finished...";
                }
                else if (_initializationStage == 1 || _initializationStage == 2)
                {
                    return $"{InitializationStageDescription} {ProgressBarProgress / ProgressBarMaximum * 100:F2}% ({ProgressBarProgress}/{ProgressBarMaximum})";
                }
                else if (_initializationStage == 3)
                {
                    return $"{InitializationStageDescription} {ProgressBarProgress / ProgressBarMaximum * 100:F2}% ({FileData.FormatFileSize(ProgressBarProgress)}/{FileData.FormatFileSize(ProgressBarMaximum)})";
                }
                else
                {
                    throw new Exception($"Unknown initializationStage: {_initializationStage}");
                }
            } 
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressBarText))]
        private double _progressBarProgress = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressBarText))]
        private double _progressBarMaximum = 100;

        public bool IsProgressBarIndeterminate => InitializationStageDescription == string.Empty;


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BackgroundBlurRadius))]
        private bool _isDeduplicationConfirmationPopupOpen = false;

        public int BackgroundBlurRadius => IsDeduplicationConfirmationPopupOpen ? 16 : 0;

        [ObservableProperty]
        private DuplicateFileGroup? _currentDeduplicationTarget = null;

        [ObservableProperty]
        private string _deduplicationTitleText = string.Empty;

        [ObservableProperty]
        private FileData? _fileToBeKept;

        [ObservableProperty]
        private ObservableCollection<FileData>? _filesToBeDeleted;

        private Action _deduplicationCallback;

        public DuplicateFilesViewModel(DirectoryData topDirectoryData, Action deduplicationCallback, INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
            
            TopDirectoryData = topDirectoryData;

            ExtensionFilterViewModel = new ExtensionFilterViewModel((bool regularFilteringPass) =>
            {
                Task.Run(() => FindDuplicates(regularFilteringPass ? ExtensionFilterViewModel : null));
            });

            TreeDataGridSource = new HierarchicalTreeDataGridSource<IFilesystemEntry>(TopDirectoryData);
            DuplicateGroups = new ObservableCollection<DuplicateFileGroup>();

            _deduplicationCallback = deduplicationCallback;

            CancellationTokenSource = new CancellationTokenSource();
        }

        public void FindDuplicates(ExtensionFilterViewModel? extensionFilterViewModel = null)
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource = new CancellationTokenSource();

            InitializationFinished = false;
            DuplicateGroups.Clear();

            Dictionary<long, List<FileData>> listsOfFilesWithSameSize = new Dictionary<long, List<FileData>>();
            Dictionary<(long, string), List<FileData>> listsOfFilesWithSameFingerprintAndSize = new Dictionary<(long, string), List<FileData>>();
            Dictionary<(long, string), List<FileData>> listsOfFilesWithSameHashAndSize = new Dictionary<(long, string), List<FileData>>();

            _initializationStage = 1;
            InitializationStageDescription = "Stage 1/3: Grouping files by size";
            ProgressBarProgress = 0;
            ProgressBarMaximum = TopDirectoryData.FilesInside;
            ProcessDirectoryData(TopDirectoryData);

            _initializationStage = 2;
            InitializationStageDescription = "Stage 2/3: Computing fingerprints for files with same size";
            ProgressBarProgress = 0;
            ProgressBarMaximum = listsOfFilesWithSameSize.Sum(kvPair => kvPair.Value.Count);
            foreach (KeyValuePair<long, List<FileData>> kvPair in listsOfFilesWithSameSize)
            {
                if (kvPair.Value.Count < 2)
                {
                    ProgressBarProgress = ProgressBarProgress + kvPair.Value.Count;
                    continue;
                }

                foreach (FileData fileData in kvPair.Value)
                {
                    if (CancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    (long, string) fileSizeAndFingerprint;
                    try
                    {
                        fileSizeAndFingerprint = (fileData.LogicalSize, fileData.Fingerprint);
                    }
                    catch (Exception ex)
                    {
                        Notification errorNotification = new Notification()
                        {
                            Type = NotificationType.Error,
                            Title = "Fingerprint computation error",
                            Message = $"{ex.Message}",
                        };

                        Dispatcher.UIThread.InvokeAsync(() => _notificationManager.Show(errorNotification));
                        ProgressBarProgress++;
                        continue;
                    }

                    if (listsOfFilesWithSameFingerprintAndSize.ContainsKey(fileSizeAndFingerprint))
                    {
                        listsOfFilesWithSameFingerprintAndSize[fileSizeAndFingerprint].Add(fileData);
                    }
                    else
                    {
                        listsOfFilesWithSameFingerprintAndSize.Add(fileSizeAndFingerprint, new List<FileData>());
                        listsOfFilesWithSameFingerprintAndSize[fileSizeAndFingerprint].Add(fileData);
                    }
                    ProgressBarProgress++;
                }
            }

            _initializationStage = 3;
            InitializationStageDescription = "Stage 3/3: Computing hashes for files with same size and fingerprint";
            ProgressBarProgress = 0;
            ProgressBarMaximum = listsOfFilesWithSameFingerprintAndSize.Sum(kvPair => kvPair.Value.Sum(file => file.LogicalSize));
            foreach (KeyValuePair<(long, string), List<FileData>> kvPair in listsOfFilesWithSameFingerprintAndSize)
            {
                if (kvPair.Value.Count < 2)
                {
                    ProgressBarProgress = ProgressBarProgress + kvPair.Value.Sum(file => file.LogicalSize);
                    continue;
                }

                foreach (FileData fileData in kvPair.Value)
                {
                    if (CancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    (long, string) fileSizeAndHash;
                    try
                    {
                        fileSizeAndHash = (fileData.LogicalSize, fileData.HashMD5);
                    }
                    catch (Exception ex)
                    {
                        Notification errorNotification = new Notification()
                        {
                            Type = NotificationType.Error,
                            Title = "Hash computation error",
                            Message = $"{ex.Message}",
                        };

                        Dispatcher.UIThread.InvokeAsync(() => _notificationManager.Show(errorNotification));
                        ProgressBarProgress = ProgressBarProgress + fileData.LogicalSize;
                        continue;
                    }

                    if (listsOfFilesWithSameHashAndSize.ContainsKey(fileSizeAndHash))
                    {
                        listsOfFilesWithSameHashAndSize[fileSizeAndHash].Add(fileData);
                    }
                    else
                    {
                        listsOfFilesWithSameHashAndSize.Add(fileSizeAndHash, new List<FileData>());
                        listsOfFilesWithSameHashAndSize[fileSizeAndHash].Add(fileData);
                    }
                    ProgressBarProgress = ProgressBarProgress + fileData.LogicalSize;
                }
            }

            foreach (KeyValuePair<(long, string), List<FileData>> kvPair in listsOfFilesWithSameHashAndSize)
            {
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (kvPair.Value.Count < 2)
                {
                    continue;
                }

                DuplicateGroups.Add(new DuplicateFileGroup(TopDirectoryData, kvPair.Value));
            }
            DuplicateGroups = [.. DuplicateGroups.OrderByDescending(group => group.PhysicalSize)];
            OnPropertyChanged(nameof(AreThereAnyDuplicateGroups));

            RefreshTreeDataGrid();
            InitializationFinished = true;

            void ProcessDirectoryData(DirectoryData directoryData)
            {
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                foreach (FileData fileData in directoryData.Files)
                {
                    if (extensionFilterViewModel is not null && !extensionFilterViewModel.DoesFilePassFilter(fileData))
                    {
                        continue;
                    }

                    long fileSize = fileData.LogicalSize;
                    if (listsOfFilesWithSameSize.Keys.Contains(fileSize))
                    {
                        listsOfFilesWithSameSize[fileSize].Add(fileData);
                    }
                    else
                    {
                        listsOfFilesWithSameSize.Add(fileSize, new List<FileData>());
                        listsOfFilesWithSameSize[fileSize].Add(fileData);
                    }

                    ProgressBarProgress++;
                }

                foreach (DirectoryData subDirectoryData in directoryData.Directories)
                {
                    ProcessDirectoryData(subDirectoryData);
                }
            }
        }

        private void RefreshTreeDataGrid()
        {
            if (TreeDataGridSource is not null)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TreeDataGridSource.RowSelection!.SelectionChanged -= DirectoryTreeViewModel_SelectionChanged;
                });
            }

            HierarchicalTreeDataGridSource<IFilesystemEntry> newSource = new HierarchicalTreeDataGridSource<IFilesystemEntry>(DuplicateGroups)
            {
                Columns =
                {
                    new HierarchicalExpanderColumn<IFilesystemEntry>(
                        new TemplateColumn<IFilesystemEntry>(
                            "Name",
                            "NameColumn",
                            null,
                            new GridLength(450),
                            options: new TemplateColumnOptions<IFilesystemEntry>
                            {
                                CompareAscending = (a, b) => a!.Name.CompareTo(b!.Name),
                                CompareDescending = (a, b) => b!.Name.CompareTo(a!.Name)
                            }),
                        x => x.SubNodes)
                }
            };

            newSource.WithTextColumn(
                "Allocated",
                x => x.PhysicalSizeText,
                options =>
                {
                    options.CompareAscending = (a, b) => ((IFilesystemEntry)a!).PhysicalSize.CompareTo(((IFilesystemEntry)b!).PhysicalSize);
                    options.CompareDescending = (a, b) => ((IFilesystemEntry)b!).PhysicalSize.CompareTo(((IFilesystemEntry)a!).PhysicalSize);
                });

            newSource.WithTextColumn(
                "Size",
                x => x.LogicalSizeText,
                options =>
                {
                    options.CompareAscending = (a, b) => ((IFilesystemEntry)a!).LogicalSize.CompareTo(((IFilesystemEntry)b!).LogicalSize);
                    options.CompareDescending = (a, b) => ((IFilesystemEntry)b!).LogicalSize.CompareTo(((IFilesystemEntry)a!).LogicalSize);
                });

            newSource.WithTextColumn(
                "Duplicate Count",
                x => x.FilesInsideText,
                options =>
                {
                    options.CompareAscending = (a, b) => ((IFilesystemEntry)a!).FilesInside.CompareTo(((IFilesystemEntry)b!).FilesInside);
                    options.CompareDescending = (a, b) => ((IFilesystemEntry)b!).FilesInside.CompareTo(((IFilesystemEntry)a!).FilesInside);
                });

            TreeDataGridSource = newSource;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                TreeDataGridSource.RowSelection!.SelectionChanged += DirectoryTreeViewModel_SelectionChanged;
            });


            void DirectoryTreeViewModel_SelectionChanged(object? sender, TreeDataGridSelectionChangedEventArgs<IFilesystemEntry> e)
            {
                SelectedItem = TreeDataGridSource.RowSelection.SelectedItem;
            }
        }


        [RelayCommand]
        private void ColumnMenuItemSortAscending(TreeDataGridColumnHeader header)
        {
            TreeDataGridSource.SortBy(TreeDataGridSource.Columns[header.ColumnIndex], System.ComponentModel.ListSortDirection.Ascending);
        }

        [RelayCommand]
        private void ColumnMenuItemSortDescending(TreeDataGridColumnHeader header)
        {
            TreeDataGridSource.SortBy(TreeDataGridSource.Columns[header.ColumnIndex], System.ComponentModel.ListSortDirection.Descending);
        }

        [RelayCommand]
        private void ColumnMenuItemExpandAll()
        {
            TreeDataGridSource.ExpandAll();
        }

        [RelayCommand]
        private void ColumnMenuItemCollapseAll()
        {
            TreeDataGridSource.CollapseAll();
        }

        [RelayCommand]
        private void RowMenuItemExpand(IFilesystemEntry rowEntry)
        {
            if (rowEntry is null)
            {
                return;
            }

            List<int>? path = FindPath(rowEntry);
            if (path is not null)
            {
                TreeDataGridSource.Expand(new IndexPath(path));
            }
        }

        [RelayCommand]
        private void RowMenuItemCollapse(IFilesystemEntry rowEntry)
        {
            if (rowEntry is null)
            {
                return;
            }

            List<int>? path = FindPath(rowEntry);
            if (path is not null)
            {
                TreeDataGridSource.Collapse(new IndexPath(path));
            }
        }

        private List<int>? FindPath(IFilesystemEntry target)
        {
            for (int i = 0; i < DuplicateGroups.Count; i++)
            {
                if (DuplicateGroups[i] == target)
                {
                    return [i];
                }
            }

            return null;
        }

        [RelayCommand]
        private void RowMenuItemOpen(IFilesystemEntry rowEntry)
        {
            try
            {
                FilesystemHelper.Open(rowEntry.Path);
            }
            catch (Exception ex)
            {
                string type = rowEntry is DirectoryData ? "Directory" : rowEntry is FileData ? "File" : throw new Exception($"Unsupported type: {rowEntry.GetType()}");

                _notificationManager.Show(new Notification()
                {
                    Type = NotificationType.Error,
                    Title = $"{type} open error",
                    Message = $"{ex.Message}",
                });
            }
        }

        [RelayCommand]
        private void RowMenuItemSelectInFileManager(IFilesystemEntry rowEntry)
        {
            try
            {
                FilesystemHelper.SelectInFileManager(rowEntry.Path);
            }
            catch (Exception ex)
            {
                string type = rowEntry is DirectoryData ? "Directory" : rowEntry is FileData ? "File" : throw new Exception($"Unsupported type: {rowEntry.GetType()}");

                _notificationManager.Show(new Notification()
                {
                    Type = NotificationType.Error,
                    Title = $"{type} selection error",
                    Message = $"{ex.Message}",
                });
            }
        }

        [RelayCommand]
        private async Task RowMenuItemCopyPath(IFilesystemEntry rowEntry)
        {
            try
            {
                await ClipboardHelper.CopyTextAsync(rowEntry.Path);
            }
            catch (Exception ex)
            {
                _notificationManager.Show(new Notification()
                {
                    Type = NotificationType.Error,
                    Title = "Clipboard copy error",
                    Message = $"{ex.Message}",
                });
            }
        }

        [RelayCommand]
        private void RowMenuItemDeduplicate(IFilesystemEntry rowEntry)
        {
            IsDeduplicationConfirmationPopupOpen = true;

            CurrentDeduplicationTarget = (rowEntry as DuplicateFileGroup) ?? throw new Exception("This command must be called on DuplicateFileGroup");

            FileToBeKept = (CurrentDeduplicationTarget.SubNodes[0] as FileData) ?? throw new Exception("DuplicateFileGroup can only contain FileData elements in Subnodes");
            FilesToBeDeleted = [.. CurrentDeduplicationTarget.SubNodes
                .Where(subnode => subnode is FileData && subnode != FileToBeKept && subnode.Path != FileToBeKept.Path)
                .Select(subnode => subnode as FileData)];

            DeduplicationTitleText = $"Are you sure you want to keep 1 file and delete {FilesToBeDeleted.Count} its duplicates?";
        }

        [RelayCommand]
        private void CancelDeduplication()
        {
            CurrentDeduplicationTarget = null;
            IsDeduplicationConfirmationPopupOpen = false;
        }

        [RelayCommand]
        private void PerformDeduplication()
        {
            foreach (FileData fileToDelete in FilesToBeDeleted ?? [])
            {
                try
                {
                    File.Delete(fileToDelete.Path);
                    fileToDelete.UpperDirectory.Files.Remove(fileToDelete);
                }
                catch (Exception ex)
                {
                    _notificationManager.Show(new Notification()
                    {
                        Type = NotificationType.Error,
                        Title = "Deletion error",
                        Message = $"{ex.Message}",
                    });
                }
            }
            DuplicateGroups.Remove(CurrentDeduplicationTarget!);
            OnPropertyChanged(nameof(AreThereAnyDuplicateGroups));

            if ((FilesToBeDeleted?.Count ?? 0) > 0)
            {
                _notificationManager.Show(new Notification()
                {
                    Type = NotificationType.Success,
                    Title = "Deduplication Successful",
                    Message = $"Successfully deleted {FilesToBeDeleted?.Count ?? 0} duplicates",
                });
            }

            CurrentDeduplicationTarget = null;
            IsDeduplicationConfirmationPopupOpen = false;

            _deduplicationCallback.Invoke();
        }
    }
}