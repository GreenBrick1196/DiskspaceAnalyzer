using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using RPZ_DiskspaceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.Converters
{
    public class IsExpandableConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is IFilesystemEntry entry && entry.SubNodes?.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}