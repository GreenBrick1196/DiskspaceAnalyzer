using Avalonia.Input.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.Helpers
{
    public static class ClipboardHelper
    {
        private static IClipboard? _clipboard;

        public static void Initialize(IClipboard clipboard)
        {
            _clipboard = clipboard;
        }

        public static async Task CopyTextAsync(string text)
        {
            if (_clipboard is null)
            {
                throw new InvalidOperationException("ClipboardService has not been initialized");
            }

            await _clipboard.SetTextAsync(text);
        }
    }
}