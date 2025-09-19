using System;

namespace FlexBackend.ORD.Rcl.Areas.ORD.ViewModels
{
    public class ReturnListVM
    {
        public int ReturnRequestId { get; set; }
        public string RmaNo { get; set; } = "";  // 對應 OrdReturnRequest.RmaId
        public string OrderNo { get; set; } = "";
        public string TypeName { get; set; } = "";  // RequestType
        public string ScopeName { get; set; } = "";  // RefundScope
        public string StatusName { get; set; } = "";  // Status
        public DateTime CreatedDate { get; set; }
        public string Reason { get; set; } = "";  // ReasonText

    }


}
