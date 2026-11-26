using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RPZ_DiskspaceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.ViewModels
{
    public partial class ExtensionFilterViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Extensions))]
        private string _extensionsString = string.Empty;

        public List<string> Extensions => ExtensionsString.ToLower().Split([" ", "\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries).ToList();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ApplyFilterCommand))]
        private bool _isIncludeModeChecked = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ApplyFilterCommand))]
        private bool _isExcludeModeChecked = false;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddPresetCommand))]
        private string? _selectedFiletype;

        private readonly Action<bool> _applyButtonPressedCallback;

        public ExtensionFilterViewModel(Action<bool> applyButtonPressedCallback)
        {
            _applyButtonPressedCallback = applyButtonPressedCallback;
        }

        public bool DoesFilePassFilter(FileData fileData)
        {
            string normalizedExtension = fileData.Extension.ToLower();
            return (Extensions.Contains(normalizedExtension) && IsIncludeModeChecked) || (!Extensions.Contains(normalizedExtension) && IsExcludeModeChecked);
        }

        [RelayCommand(CanExecute = nameof(CanAddPreset))]
        private void AddPreset()
        {
            string newExtensions = string.Join(" ", FileData.FileTypesToExtensions[SelectedFiletype!]);

            if (ExtensionsString != string.Empty)
            {
                ExtensionsString = ExtensionsString + Environment.NewLine + newExtensions;
            }
            else
            {
                ExtensionsString = ExtensionsString + newExtensions;
            }
        }
        private bool CanAddPreset()
        {
            return SelectedFiletype is not null;
        }

        [RelayCommand(CanExecute=nameof(CanApplyFilter))]
        private void ApplyFilter()
        {
            _applyButtonPressedCallback.Invoke(true);
        }
        private bool CanApplyFilter()
        {
            return IsIncludeModeChecked || IsExcludeModeChecked;
        }

        [RelayCommand]
        private void ClearFilter()
        {
            ExtensionsString = string.Empty;
            _applyButtonPressedCallback.Invoke(false);
        }
    }
}
