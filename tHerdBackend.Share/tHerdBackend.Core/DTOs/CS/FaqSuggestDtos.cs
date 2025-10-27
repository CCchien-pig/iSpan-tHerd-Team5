using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.CS
{
    public sealed class FaqSuggestDto
    {
        public int FaqId { get; set; }
        public string Title { get; set; } = "";
        public string CategoryName { get; set; } = "";
    }
}