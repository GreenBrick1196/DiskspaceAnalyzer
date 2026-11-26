using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore.SkiaSharpView.Avalonia;
using RPZ_DiskspaceAnalyzer.Models;
using RPZ_DiskspaceAnalyzer.ViewModels;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.Views;

public partial class DuplicateFilesView : UserControl
{
    public DuplicateFilesView()
    {
        InitializeComponent();
    }

    private void TreeDataGrid_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not TreeDataGrid grid)
        {
            return;
        }

        foreach (TreeDataGridColumnHeader header in grid.GetVisualDescendants().OfType<TreeDataGridColumnHeader>())
        {
            AttachColumnContextMenu(header, grid);
        }
    }

    private void AttachColumnContextMenu(TreeDataGridColumnHeader header, TreeDataGrid grid)
    {
        if (grid.DataContext is not DuplicateFilesViewModel vm)
        {
            return;
        }

        ContextMenu menu = new ContextMenu();

        menu.Items.Add(new MenuItem
        {
            Header = "Sort Ascending",
            Command = vm.ColumnMenuItemSortAscendingCommand,
            CommandParameter = header,
        });

        menu.Items.Add(new MenuItem
        {
            Header = "Sort Descending",
            Command = vm.ColumnMenuItemSortDescendingCommand,
            CommandParameter = header
        });

        if (header.ColumnIndex != -1 && grid?.Columns?[header.ColumnIndex] is HierarchicalExpanderColumn<IFilesystemEntry>)
        {
            menu.Items.Add(new Separator());

            menu.Items.Add(new MenuItem
            {
                Header = "Expand All",
                Command = vm.ColumnMenuItemExpandAllCommand,
            });

            menu.Items.Add(new MenuItem
            {
                Header = "Collapse All",
                Command = vm.ColumnMenuItemCollapseAllCommand,
            });
        }

        header.ContextMenu = menu;
    }

    private void WaitingProgressbar_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (waitingProgressbar.IsIndeterminate)
        {
            waitingProgressbar.IsIndeterminate = false;
            waitingProgressbar.IsIndeterminate = true;
        }
    }
}