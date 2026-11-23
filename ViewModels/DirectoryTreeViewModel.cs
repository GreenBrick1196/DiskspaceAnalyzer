using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using RPZ_DiskspaceAnalyzer.Helpers;
using RPZ_DiskspaceAnalyzer.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.ViewModels
{
    public partial class DirectoryTreeViewModel : ViewModelBase
    {
        private readonly INotificationManager _notificationManager;

        [ObservableProperty]
        private DirectoryData _topDirectoryData;

        [ObservableProperty]
        private HierarchicalTreeDataGridSource<IFilesystemEntry> _treeDataGridSource;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsItemSelected))]
        [NotifyPropertyChangedFor(nameof(IsSelectedItemDirectoryData))]
        [NotifyPropertyChangedFor(nameof(PieChartSeries))]
        [NotifyPropertyChangedFor(nameof(FilesystemEntryInfoDisplayViewModel))]
        private IFilesystemEntry? _selectedItem;

        public bool IsItemSelected => SelectedItem is not null;
        public bool IsSelectedItemDirectoryData => SelectedItem is DirectoryData;

        public FilesystemEntryInfoDisplayViewModel? FilesystemEntryInfoDisplayViewModel => SelectedItem is not null ? new FilesystemEntryInfoDisplayViewModel(SelectedItem) : null;


        partial void OnSelectedItemChanged(IFilesystemEntry? oldValue, IFilesystemEntry? newValue)
        {
            if (oldValue is not null && oldValue.SubNodes is not null)
            {
                oldValue.SubNodes.CollectionChanged -= SubNodes_CollectionChanged;
                oldValue.PropertyChanged -= NewValue_PropertyChanged;
            }

            PieChartSeries.Clear();
            if (newValue is null || newValue.SubNodes is null)
            {
                return;
            }

            newValue.SubNodes.CollectionChanged += SubNodes_CollectionChanged;
            newValue.PropertyChanged += NewValue_PropertyChanged;

            RedrawPieChart(true);

            void NewValue_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(newValue.PhysicalSize))
                {
                    RedrawPieChart(false);
                }
                if (e.PropertyName == nameof(newValue.SubNodes) || e.PropertyName == nameof(DirectoryData.StartedInitialization) || e.PropertyName == nameof(DirectoryData.FinishedInitialization))
                {
                    RedrawPieChart(true);
                }
            }

            void SubNodes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                RedrawPieChart(false);
            }
        }

        public ObservableCollection<ISeries> PieChartSeries { get; init; }

        private int _pieChartMaxItems = 25;
        private long _pieChartMinPercentageToBeShown = 4;

        [ObservableProperty]
        private TimeSpan _pieChartAnimationsSpeed = TimeSpan.FromSeconds(0.5);

        private TimeSpan _pieChartThrottleTime = TimeSpan.FromSeconds(3);
        private long _lastPieChartChartRebuildTimestamp;

        private void RedrawPieChart(bool ignoreThrottling)
        {
            if (!ignoreThrottling && Stopwatch.GetElapsedTime(_lastPieChartChartRebuildTimestamp) < _pieChartThrottleTime)
            {
                return;
            }

            if (SelectedItem?.SubNodes is null)
            {
                return;
            }

            IEnumerable<IFilesystemEntry> allEntriesSorted = SelectedItem.SubNodes.OrderByDescending(entry => entry.PhysicalSize);
            IEnumerable<IFilesystemEntry> entriesToShow = allEntriesSorted.Where(entry => entry.PhysicalSizeDirectoryPercentage >= _pieChartMinPercentageToBeShown).Take(Math.Min(_pieChartMaxItems, SelectedItem.SubNodes.Count));
            
            IEnumerable<IFilesystemEntry> otherEntries = allEntriesSorted.Where(entry => !entriesToShow.Contains(entry));
            long otherPhysicalSizesSum = 0;
            long otherDirectoryCount = 0;
            long otherFileCount = 0;

            foreach (IFilesystemEntry otherEntry in otherEntries)
            {
                otherPhysicalSizesSum = otherPhysicalSizesSum + otherEntry.PhysicalSize;

                if (otherEntry is DirectoryData)
                {
                    otherDirectoryCount++;
                }
                else if (otherEntry is FileData)
                {
                    otherFileCount++;
                }
                else
                {
                    throw new Exception($"Unsupported type: {otherEntry.GetType()}");
                }
            }

            PieChartSeries.Clear();

            foreach (IFilesystemEntry entry in entriesToShow)
            {
                PieChartSeries.Add(new PieSeries<double>
                {
                    Name = entry.Name,
                    Values = new ObservableCollection<double> { entry.PhysicalSize },
                    ToolTipLabelFormatter = point => $"{(entry is DirectoryData ? "Directory" : "File")}, {FileData.FormatFileSize(entry.PhysicalSize)}",
                });
            }

            if (otherPhysicalSizesSum > 0)
            {
                PieChartSeries.Add(new PieSeries<double>
                {
                    Name = "Other",
                    Values = new ObservableCollection<double> { otherPhysicalSizesSum },
                    ToolTipLabelFormatter = point => $"Other ({otherDirectoryCount} directories, {otherFileCount} files), {FileData.FormatFileSize(otherPhysicalSizesSum)}",

                    Fill = new SolidColorPaint(SKColors.Gray)
                });
            }

            _lastPieChartChartRebuildTimestamp = Stopwatch.GetTimestamp();
        }

        public DirectoryTreeViewModel(DirectoryData topDirectoryData, INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;

            TopDirectoryData = topDirectoryData;

            PieChartSeries = new ObservableCollection<ISeries>();

            TreeDataGridSource = null!;
            RefreshTreeDataGrid();
            TreeDataGridSource.Expand(0);
        }

        public void RefreshTreeDataGrid()
        {
            if (TreeDataGridSource is not null)
            {
                TreeDataGridSource.RowSelection!.SelectionChanged -= DirectoryTreeViewModel_SelectionChanged;
            }

            HierarchicalTreeDataGridSource<IFilesystemEntry> newSource = new HierarchicalTreeDataGridSource<IFilesystemEntry>(TopDirectoryData)
            {
                Columns =
                {
                    new HierarchicalExpanderColumn<IFilesystemEntry>(
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
                        x => x.SubNodes)
                }
            };

            newSource.WithTemplateColumnFromResourceKeys(
                "Proportion",
                "ProportionColumn", 
                null,
                options =>
                {
                    options.CompareAscending = (a, b) => ((IFilesystemEntry)a!).PhysicalSizeTotalPercentage.CompareTo(((IFilesystemEntry)b!).PhysicalSizeTotalPercentage);
                    options.CompareDescending = (a, b) => ((IFilesystemEntry)b!).PhysicalSizeTotalPercentage.CompareTo(((IFilesystemEntry)a!).PhysicalSizeTotalPercentage);
                });

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
                "Directories Inside",
                x => x.DirectoriesInsideText,
                options =>
                {
                    options.CompareAscending = (a, b) => ((IFilesystemEntry)a!).DirectoriesInside.CompareTo(((IFilesystemEntry)b!).DirectoriesInside);
                    options.CompareDescending = (a, b) => ((IFilesystemEntry)b!).DirectoriesInside.CompareTo(((IFilesystemEntry)a!).DirectoriesInside);
                });

            newSource.WithTextColumn(
                "Files Inside",
                x => x.FilesInsideText,
                options =>
                {
                    options.CompareAscending = (a, b) => ((IFilesystemEntry)a!).FilesInside.CompareTo(((IFilesystemEntry)b!).FilesInside);
                    options.CompareDescending = (a, b) => ((IFilesystemEntry)b!).FilesInside.CompareTo(((IFilesystemEntry)a!).FilesInside);
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

            TreeDataGridSource.RowSelection!.SelectionChanged += DirectoryTreeViewModel_SelectionChanged;


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

            List<int>? path = FindPath(rowEntry, TopDirectoryData);
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

            List<int>? path = FindPath(rowEntry, TopDirectoryData);
            if (path is not null)
            {
                TreeDataGridSource.Collapse(new IndexPath(path));
            }
        }

        private static List<int>? FindPath(IFilesystemEntry target, IFilesystemEntry currentParentEntry, List<int>? currentParentPath = null)
        {
            currentParentPath ??= [0];
            if (currentParentEntry == target)
            {
                return currentParentPath;
            }

            for (int i = 0; i < currentParentEntry.SubNodes?.Count; i++)
            {
                if (i == 0)
                {
                    currentParentPath = currentParentPath.Append(0).ToList();
                }
                currentParentPath[currentParentPath.Count - 1] = i;

                if (ReferenceEquals(currentParentEntry.SubNodes[i], target))
                {
                    return currentParentPath;
                }

                List<int>? subPath = FindPath(target, currentParentEntry.SubNodes[i], currentParentPath);
                if (subPath is not null)
                {
                    return subPath;
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
    }
}
