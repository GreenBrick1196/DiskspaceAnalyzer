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
        
        private static readonly IImage DuplicateFileGroupIcon = new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/DuplicateFileGroupIcon.svg"))) };

        private static readonly IImage UnknownIcon = new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/UnknownIcon.svg"))) };

        private static readonly IImage FileImageIcon = new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/FileImageIcon.svg"))) };
        private static readonly IImage FileVideoIcon = new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/FileVideoIcon.svg"))) };
        private static readonly IImage FileAudioIcon = new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/FileAudioIcon.svg"))) };
        private static readonly IImage FileTextIcon =  new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/FileTextIcon.svg"))) };
        private static readonly IImage FileOtherIcon = new SvgImage { Source = SvgSource.LoadFromStream(AssetLoader.Open(new Uri("avares://RPZ_DiskspaceAnalyzer/Assets/FileOtherIcon.svg"))) };

        private static readonly string[] ImageExtenstions = [".jpg", ".jpeg", ".jfif", ".png", ".gif", ".webp", ".avif", ".tiff", ".tif", ".bmp", ".heif", ".heic"];
        private static readonly string[] VideoExtenstions = [".mp4", ".mkv", ".webm", ".mov", ".avi", ".wmv"];
        private static readonly string[] AudioExtenstions = [".mp3", ".aac", ".m4a", ".ogg", ".opus", ".wav", ".flac", ".alac", ".ape"];
        private static readonly string[] TextExtenstions = [".txt", ".md", ".markdown", ".csv", ".json", ".xml", ".log", ".doc", ".docx", ".odt", ".rtf", ".pdf", ".html"];

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

                if (ImageExtenstions.Contains(normalizedExtension))
                {
                    return FileImageIcon;
                }
                else if (VideoExtenstions.Contains(normalizedExtension))
                {
                    return FileVideoIcon;
                }
                else if (AudioExtenstions.Contains(normalizedExtension))
                {
                    return FileAudioIcon;
                }
                else if (TextExtenstions.Contains(normalizedExtension))
                {
                    return FileTextIcon;
                }
                else
                {
                    return FileOtherIcon;
                }
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
