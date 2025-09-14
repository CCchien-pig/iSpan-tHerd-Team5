using System;
using System.Collections.Generic;

namespace FlexBackend.ORD.Rcl.Areas.ORD.ViewModels.Returns
{
    public class ReturnListItemVM
    {
        public int ReturnRequestId { get; set; }
        public string RmaId { get; set; } = "";
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public string RequestType { get; set; } = "";    
        public string RefundScope { get; set; } = "";     
        public string Status { get; set; } = "";          

        public string RequestTypeName { get; set; } = "";
        public string RefundScopeName { get; set; } = "";
        public string StatusName { get; set; } = "";

        public DateTime CreatedDate { get; set; }
        public string? ReasonText { get; set; }
    }

    public class ReturnTabsCountVM
    {
        public int All { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Done { get; set; }
        public int Rejected { get; set; }
    }

    public class ReturnListPageVM
    {
        public string? Group { get; set; }     // null / pending / approved / rejected
        public string? Keyword { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int Total { get; set; }
        public int Pages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));

        public List<ReturnListItemVM> Items { get; set; } = new();
        public ReturnTabsCountVM Tabs { get; set; } = new();
    }

    public class ReturnDetailVM
    {
        public int ReturnRequestId { get; set; }
        public string RmaId { get; set; } = "";
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public string RequestTypeName { get; set; } = "";
        public string RefundScopeName { get; set; } = "";
        public string StatusName { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public string? ReasonText { get; set; }
        public List<ReturnDetailItemVM> Items { get; set; } = new();
    }

    public class ReturnDetailItemVM
    {
        public int RmaItemId { get; set; }
        public int OrderItemId { get; set; }
        public int Qty { get; set; }
        public int ApprovedQty { get; set; }
        public int RefundQty { get; set; }
        public int ReshipQty { get; set; }
        public decimal? RefundUnitAmount { get; set; }
    }

}
