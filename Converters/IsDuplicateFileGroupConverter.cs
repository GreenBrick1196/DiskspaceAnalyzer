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
    public class IsDuplicateFileGroupConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool result = value is DuplicateFileGroup;

            if (parameter?.ToString() == "Not")
            {
                result = !result;
            }

            return result;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
