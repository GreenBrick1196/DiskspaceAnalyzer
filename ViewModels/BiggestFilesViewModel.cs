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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.ViewModels
{
    public partial class BiggestFilesViewModel : ViewModelBase
    {
        private INotificationManager _notificationManager;

        [ObservableProperty]
        private DirectoryData _topDirectoryData;

        [ObservableProperty]
        private FlatTreeDataGridSource<IFilesystemEntry> _treeDataGridSource;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreThereAnyFiles))]
        private ObservableCollection<FileData> _filesToShow;

        public bool AreThereAnyFiles => FilesToShow.Count > 0;

        [ObservableProperty]
        private bool _initializationFinished = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsItemSelected))]
        [NotifyPropertyChangedFor(nameof(FilesystemEntryInfoDisplayViewModel))]
        private IFilesystemEntry? _selectedItem;

        public bool IsItemSelected => SelectedItem is not null;
        public FilesystemEntryInfoDisplayViewModel? FilesystemEntryInfoDisplayViewModel => SelectedItem is not null ? new FilesystemEntryInfoDisplayViewModel(SelectedItem) : null;


        public BiggestFilesViewModel(DirectoryData topDirectoryData, INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;

            _topDirectoryData = topDirectoryData;

            _filesToShow = new ObservableCollection<FileData>();

            TreeDataGridSource = new FlatTreeDataGridSource<IFilesystemEntry>(FilesToShow);
        }

        public void FindBiggestFiles()
        {
            List<FileData> allFiles = new List<FileData>();

            ProcessDirectoryData(TopDirectoryData);
            allFiles.Sort((a, b) => b.PhysicalSize.CompareTo(a.PhysicalSize));

            FilesToShow.Clear();
            for (int i = 0; i < Math.Min(allFiles.Count, 10000); i++)
            {
                FilesToShow.Add(allFiles[i]);
            }

            RefreshTreeDataGrid();
            InitializationFinished = true;


            void ProcessDirectoryData(DirectoryData directoryData)
            {
                foreach (FileData fileData in directoryData.Files)
                {
                    allFiles.Add(fileData);
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

            FlatTreeDataGridSource<IFilesystemEntry> newSource = new FlatTreeDataGridSource<IFilesystemEntry>(FilesToShow)
            {
                Columns =
                {
                    new TemplateColumn<IFilesystemEntry>(
                        "Name",
                        "NameColumn",
                        null,
                        new GridLength(400),
                        options: new TemplateColumnOptions<IFilesystemEntry>
                        {
                            CompareAscending = (a, b) => a!.Name.CompareTo(b!.Name),
                            CompareDescending = (a, b) => b!.Name.CompareTo(a!.Name)
                        }),
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
                "Last Edited At",
                x => x.EditedTimeText,
                options =>
                {
                    options.CompareAscending = (a, b) => ((IFilesystemEntry)a!).EditedTime.CompareTo(((IFilesystemEntry)b!).EditedTime);
                    options.CompareDescending = (a, b) => ((IFilesystemEntry)b!).EditedTime.CompareTo(((IFilesystemEntry)a!).EditedTime);
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
            ((ITreeDataGridSource)TreeDataGridSource).SortBy(TreeDataGridSource.Columns[header.ColumnIndex], System.ComponentModel.ListSortDirection.Ascending);
        }

        [RelayCommand]
        private void ColumnMenuItemSortDescending(TreeDataGridColumnHeader header)
        {
            ((ITreeDataGridSource)TreeDataGridSource).SortBy(TreeDataGridSource.Columns[header.ColumnIndex], System.ComponentModel.ListSortDirection.Descending);
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
    }
}
