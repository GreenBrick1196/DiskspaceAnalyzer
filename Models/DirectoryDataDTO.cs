using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPZ_DiskspaceAnalyzer.Models
{
    public sealed class DirectoryDataDTO
    {
        public required string Path { get; set; }

        public required long LogicalSize { get; set; }
        public required long PhysicalSize { get; set; }

        public required int DirectoriesInside { get; set; }
        public required int FilesInside { get; set; }

        public required bool StartedInitialization { get; set; }
        public required bool FinishedInitialization { get; set; }

        public required DateTime CreatedTime { get; set; }
        public required DateTime EditedTime { get; set; }

        public required List<DirectoryDataDTO> Directories { get; set; }
        public required List<FileDataDTO> Files { get; set; }
    }
}
