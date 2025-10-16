using System.Collections.Generic;

namespace tHerdBackend.ORD.Rcl.Areas.ORD.ViewModels
{
    public class ReturnListPageVM
    {
        public string? Keyword { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int Pages { get; set; }
        public int Total { get; set; }
        public string Group { get; set; } = "all";
        public Dictionary<string, int> Tabs { get; set; } = new();
        public List<ReturnListVM> RmaList { get; set; } = new();
    }
}
