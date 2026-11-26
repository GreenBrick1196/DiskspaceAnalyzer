using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using RPZ_DiskspaceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Svg.Skia;
using Avalonia.Platform;

namespace RPZ_DiskspaceAnalyzer.Converters
{
    public class FilesystemEntryToIconConverter : IValueConverter
    {
        private static readonly IImage DirectoryDiskIcon = new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/DirectoryDiskIcon.svg"))) };
        private static readonly IImage DirectoryFolderIcon = new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/DirectoryFolderIcon.svg"))) };

        private static readonly Dictionary<string, IImage> _fileTypesToIcons = LoadIcons();
        private static Dictionary<string, IImage> LoadIcons()
        {
            Dictionary<string, IImage> result = new Dictionary<string, IImage>();

            foreach (string fileType in FileData.FileTypesToExtensions.Keys)
            {
                result.Add(fileType, new SvgImage
                {
                    Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri($"avares://RPZ_DiskspaceAnalyzer/Assets/File{fileType}Icon.svg")))
                });
            }
            return result;
        }

        private static readonly IImage FileOtherIcon = new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/FileOtherIcon.svg"))) };

        private static readonly IImage DuplicateFileGroupIcon = new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/DuplicateFileGroupIcon.svg"))) };

        private static readonly IImage UnknownIcon = new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/UnknownIcon.svg"))) };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DirectoryData directoryData)
            {
                if (directoryData.Path.EndsWith(":"))
                {
                    return DirectoryDiskIcon;
                }
                else
                {
                    return DirectoryFolderIcon;
                }
            }
            else if (value is DuplicateFileGroup)
            {
                return DuplicateFileGroupIcon;
            }
            else if (value is FileData filedata)
            {
                string normalizedExtension = filedata.Extension.ToLower();

                foreach (KeyValuePair<string, string[]> TypeToExtensions in FileData.FileTypesToExtensions)
                {
                    if (TypeToExtensions.Value.Contains(normalizedExtension))
                    {
                        return _fileTypesToIcons[TypeToExtensions.Key];
                    }
                }

                return FileOtherIcon;
            }
            else
            {
                return UnknownIcon;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
