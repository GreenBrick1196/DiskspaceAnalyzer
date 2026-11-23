using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.Models
{
    public sealed class FileDataDTO
    {
        public required string Path { get; set; }

        public required long LogicalSize { get; set; }
        public required long PhysicalSize { get; set; }

        public required DateTime CreatedTime { get; set; }
        public required DateTime EditedTime { get; set; }

        public required string Fingerprint { get; set; }
        public required string HashMD5 { get; set; }
    }
}
