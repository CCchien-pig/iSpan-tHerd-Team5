using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.SYS
{
    public class FileMetaUpdateDto
    {
        public int FileId { get; set; }
        public string Field { get; set; } = "";
        public string? Value { get; set; } = "";
    }
}