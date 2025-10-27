using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.CS
{
    public sealed class FaqDetailDto
    {
        public int FaqId { get; set; }
        public string Title { get; set; } = "";
        public string AnswerHtml { get; set; } = "";
        public string CategoryName { get; set; } = "";
    }
}
