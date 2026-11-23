using CommunityToolkit.Mvvm.ComponentModel;
using RPZ_DiskspaceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.ViewModels
{
    public partial class FilesystemEntryInfoDisplayViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LowerString))]
        private IFilesystemEntry _filesystemEntry;

        public string LowerString 
        { 
            get
            {
                if (FilesystemEntry is FileData filedata)
                {
                    return $"\nAllocated {FileData.FormatFileSize(filedata.PhysicalSize)} ({filedata.PhysicalSize} bytes), Size {FileData.FormatFileSize(filedata.LogicalSize)} ({filedata.LogicalSize} bytes)" +
                        $"\nTakes {filedata.PhysicalSizeDirectoryPercentageText} of directory allocated space, {filedata.PhysicalSizeTotalPercentageText} total" +
                        $"\nTakes {filedata.LogicalSizeDirectoryPercentageText} of directory size, {filedata.LogicalSizeTotalPercentageText} total" +
                        $"\nCreated {filedata.CreatedTimeText}, Edited {filedata.EditedTimeText}";
                }
                else if (FilesystemEntry is DirectoryData directoryData)
                {
                    return $"\nAllocated {FileData.FormatFileSize(directoryData.PhysicalSize)}, Size {FileData.FormatFileSize(directoryData.LogicalSize)}" +
                        $"\nTakes {directoryData.PhysicalSizeDirectoryPercentageText} of directory allocated space, {directoryData.PhysicalSizeTotalPercentageText} total" +
                        $"\nTakes {directoryData.LogicalSizeDirectoryPercentageText} of directory size, {directoryData.LogicalSizeTotalPercentageText} total" +
                        $"\nHas {directoryData.DirectoriesInside} subdirectories ({directoryData.Directories.Count} of them direct)" +
                        $"\nHas {directoryData.FilesInside} files ({directoryData.Files.Count} of them direct) " +
                        $"\nCreated {directoryData.CreatedTimeText}, Edited {directoryData.EditedTimeText}";
                }
                else if (FilesystemEntry is DuplicateFileGroup duplicateFileGroup)
                {
                    return $"\nAllocated {FileData.FormatFileSize(duplicateFileGroup.PhysicalSize)} total, {FileData.FormatFileSize(duplicateFileGroup.SubNodes[0].PhysicalSize)} per file" +
                        $"\nSize {FileData.FormatFileSize(duplicateFileGroup.LogicalSize)} total, {FileData.FormatFileSize(duplicateFileGroup.SubNodes[0].LogicalSize)} per file" +
                        $"\nConsists of {duplicateFileGroup.FilesInside} duplicates";
                }
                else
                {
                    throw new Exception($"Unknown filesystem entry type: {FilesystemEntry.GetType()}");
                }
            }
        }

        public FilesystemEntryInfoDisplayViewModel(IFilesystemEntry entry)
        {
            _filesystemEntry = entry;
        }
    }
}
