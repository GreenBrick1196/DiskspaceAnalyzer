using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RPZ_DiskspaceAnalyzer.Helpers;
using RPZ_DiskspaceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IDirectoryTreeProvider _directoryTreeProvider = new RecursiveDirectoryScanner();
        private readonly INotificationManager _notificationManager;

        private DirectoryData? _topDirectoryData;


        [ObservableProperty]
        private string _directoryPathInput = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BackgroundBlurRadius))]
        private bool _isSelectDirectoryPopupOpen = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BackgroundBlurRadius))]
        [NotifyPropertyChangedFor(nameof(ScanPopupBlurRadius))]
        private bool _isAboutPopupOpen = false;

        public int BackgroundBlurRadius => IsSelectDirectoryPopupOpen || IsAboutPopupOpen ? 16 : 0;
        public int ScanPopupBlurRadius => IsAboutPopupOpen ? 16 : 0;

        [ObservableProperty]
        private DirectoryTreeViewModel? _directoryTreeViewModel;

        [ObservableProperty]
        private BiggestFilesViewModel? _biggestFilesViewModel;

        [ObservableProperty]
        private DuplicateFilesViewModel? _duplicateFilesViewModel;

        [ObservableProperty]
        private int _selectedTabIndex;


        private CancellationTokenSource _сtsDirectoryTreeScanTask = new CancellationTokenSource(); // needs to not allow initial scan + refresh of an element at the same time
                                                                                                   // because refresh would set InitializationFinished to true for all involved
                                                                                                   // directories while they are not fully scanned by initial scan yet
        
        public MainWindowViewModel(INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
        }

        [RelayCommand]
        private void StartScan()
        {
            try
            {
                _topDirectoryData = new DirectoryData(null, null, DirectoryPathInput);
            }
            catch (Exception ex)
            {
                _notificationManager.Show(new Notification()
                {
                    Type = NotificationType.Error,
                    Title = "Error",
                    Message = $"{ex.Message}",
                });
                return;
            }

            _topDirectoryData.PropertyChanged += TopDirectoryData_PropertyChanged;

            IsSelectDirectoryPopupOpen = false;

            DirectoryTreeViewModel = new DirectoryTreeViewModel(_topDirectoryData, _notificationManager);
            BiggestFilesViewModel = new BiggestFilesViewModel(_topDirectoryData, _notificationManager);
            DuplicateFilesViewModel = new DuplicateFilesViewModel(_topDirectoryData, () => { DirectoryTreeViewModel.RefreshTreeDataGrid(); BiggestFilesViewModel.FindBiggestFiles(); }, _notificationManager);

            long startTimestamp = Stopwatch.GetTimestamp(); //make notification "Scan finished" "took X time"?
            Task.Run(() => _directoryTreeProvider.FillDirectoryTree(_topDirectoryData, _topDirectoryData, _сtsDirectoryTreeScanTask.Token, _notificationManager));
        }

        void TopDirectoryData_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_topDirectoryData is null)
            {
                return;
            }

            if (e.PropertyName == nameof(_topDirectoryData.FinishedInitialization))
            {
                if (_topDirectoryData.FinishedInitialization) // FillDirectoryTree() or RefreshElement() finished 
                {
                    DuplicateFilesViewModel?.CancellationTokenSource.Cancel();

                    BiggestFilesViewModel?.FindBiggestFiles();
                    Task.Run(() => DuplicateFilesViewModel?.FindDuplicates());
                }
                else // initialization end rolled back by refreshing an element
                {
                    DuplicateFilesViewModel?.CancellationTokenSource.Cancel();
                }
            }
        }

        [RelayCommand]
        private async Task OpenFolderPicker(Window parentWindow)
        {
            try
            {
                IReadOnlyList<IStorageFolder> pickedFolders = await parentWindow.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions
                    {
                        AllowMultiple = false,
                        Title = "Select a directory"
                    });

                if (pickedFolders.Count > 0)
                {
                    DirectoryPathInput = pickedFolders[0].TryGetLocalPath() ?? pickedFolders[0].Path.ToString();
                }
            }
            catch (Exception ex)
            {
                _notificationManager.Show(new Notification()
                {
                    Type = NotificationType.Error,
                    Title = "Error",
                    Message = $"{ex.Message}",
                });
            }
        }

        [RelayCommand]
        private void PromptNewScan()
        {
            _сtsDirectoryTreeScanTask.Cancel();
            _сtsDirectoryTreeScanTask = new CancellationTokenSource();

            IsSelectDirectoryPopupOpen = true;
        }

        [RelayCommand(CanExecute=(nameof(CanExportToJson)))]
        private async Task ExportToJson(Window parentWindow)
        {
            try
            {
                IStorageFile? pickedFile = await parentWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
                {
                    Title = "Select a file",
                    FileTypeChoices = [FilePickerFileTypes.Json]
                });

                if (pickedFile is null)
                {
                    throw new Exception("No files picked");
                }

                string filePathInput = pickedFile.TryGetLocalPath() ?? pickedFile.Path.ToString();

                DirectoryData? directoryData = DirectoryTreeViewModel?.TopDirectoryData;
                if (directoryData is null)
                {
                    throw new Exception("No data to export");
                }

                using (FileStream outputStream = File.OpenWrite(filePathInput))
                {
                    await directoryData.ToJson(outputStream);
                }

                _notificationManager.Show(new Notification()
                {
                    Type = NotificationType.Success,
                    Title = "Success",
                    Message = $"Exported data to {filePathInput}",
                });
            }
            catch (Exception ex)
            {
                _notificationManager.Show(new Notification()
                {
                    Type = NotificationType.Error,
                    Title = "Error",
                    Message = $"{ex.Message}",
                });
            }
        }
        private bool CanExportToJson()
        {
            return true;
        }


        [RelayCommand]
        private async Task ImportFromJson(Window parentWindow)
        {
            try
            {
                IReadOnlyList<IStorageFile> pickedFiles = await parentWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    AllowMultiple = false,
                    Title = "Select a file",
                    FileTypeFilter = [FilePickerFileTypes.Json]
                });

                string filePathInput;
                if (pickedFiles.Count > 0)
                {
                    filePathInput = pickedFiles[0].TryGetLocalPath() ?? pickedFiles[0].Path.ToString();
                }
                else
                {
                    throw new Exception("No files picked");
                }

                using (FileStream inputStream = File.OpenRead(filePathInput))
                {
                    _topDirectoryData = await DirectoryData.FromJson(inputStream);
                }

                if (_topDirectoryData is null)
                {
                    throw new Exception("File is invalid or corrupted");
                }

                _topDirectoryData.PropertyChanged += TopDirectoryData_PropertyChanged;

                IsSelectDirectoryPopupOpen = false;

                DirectoryTreeViewModel = new DirectoryTreeViewModel(_topDirectoryData, _notificationManager);
                BiggestFilesViewModel = new BiggestFilesViewModel(_topDirectoryData, _notificationManager);
                DuplicateFilesViewModel = new DuplicateFilesViewModel(_topDirectoryData, () => { DirectoryTreeViewModel.RefreshTreeDataGrid(); BiggestFilesViewModel.FindBiggestFiles(); }, _notificationManager);

                TopDirectoryData_PropertyChanged(null, new System.ComponentModel.PropertyChangedEventArgs(nameof(_topDirectoryData.FinishedInitialization)));

                _notificationManager.Show(new Notification()
                {
                    Type = NotificationType.Success,
                    Title = "Success",
                    Message = $"Imported data from {filePathInput}",
                });
            }
            catch (Exception ex)
            {
                _notificationManager.Show(new Notification()
                {
                    Type = NotificationType.Error,
                    Title = "Error",
                    Message = $"{ex.Message}",
                });
            }
        }

        [RelayCommand]
        private void Exit()
        {
            Environment.Exit(0);
        }

        [RelayCommand]
        private void SwitchToDirectoryTree()
        {
            SelectedTabIndex = 0;
        }

        [RelayCommand]
        private void SwitchToBiggestFiles()
        {
            SelectedTabIndex = 1;
        }

        [RelayCommand]
        private void SwitchToDuplicateFiles()
        {
            SelectedTabIndex = 2;
        }

        [RelayCommand]
        private static void SwitchTheme(string theme)
        {
            Application app = Application.Current!;

            if (theme == "Light")
            {
                app.RequestedThemeVariant = ThemeVariant.Light;
            }
            else if (theme == "Dark")
            {
                app.RequestedThemeVariant = ThemeVariant.Dark;
            }
            else
            {
                throw new Exception($"Unknown theme: {theme}");
            }
        }

        [RelayCommand]
        private void OpenAboutPopup()
        {
            IsAboutPopupOpen = true;
        }

        [RelayCommand]
        private void CloseAboutPopup()
        {
            IsAboutPopupOpen = false;
        }
    }
}