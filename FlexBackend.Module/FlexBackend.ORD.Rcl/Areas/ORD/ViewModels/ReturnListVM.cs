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

        // 列表中顯示第一筆品項（為確保可編譯，先以 ID 顯示）
        public string ProductName { get; set; } = "";  // 例：PID:123
        public string Spec { get; set; } = "";  // 例：SKU:456
        public int Qty { get; set; }
    }


}
