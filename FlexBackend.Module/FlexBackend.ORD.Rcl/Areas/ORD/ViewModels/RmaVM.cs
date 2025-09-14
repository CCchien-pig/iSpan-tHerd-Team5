using System;
using System.Collections.Generic;

namespace FlexBackend.ORD.Rcl.Areas.ORD.ViewModels
{
    public class RmaRequestRowVM
    {
        public int ReturnRequestId { get; set; }
        public string? RmaId { get; set; }              // string (實際欄位)
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";

        // 原始代碼
        public string? RequestType { get; set; }        // 08
        public string? RefundScope { get; set; }        // 10
        public string? ReasonCode { get; set; }         // 09
        public string Status { get; set; } = "pending"; // 06

        // 轉中文後
        public string? RequestTypeName { get; set; }
        public string? RefundScopeName { get; set; }
        public string? ReasonName { get; set; }
        public string? StatusName { get; set; }

        public string? ReasonText { get; set; }

        public int? CreatorId { get; set; }             // int? (UserNumberId)
        public string? CreatorName { get; set; }        // 顯示用（之後用字典轉）

        public DateTime CreatedDate { get; set; }
        public DateTime? RevisedDate { get; set; }

        public int ItemCount { get; set; }
        public bool HasRefundPayment { get; set; }
    }

    public class RmaListRowVM
    {
        public int ReturnRequestId { get; set; }
        public string? RmaId { get; set; }              // string?
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";

        public string Status { get; set; } = "review";  // 非 pending/rejected 皆視為流程中
        public string? StatusName { get; set; }

        public int? ReviewerId { get; set; }            // int?
        public string? ReviewerName { get; set; }       // 顯示用
        public DateTime? ReviewedDate { get; set; }
        public string? ReviewComment { get; set; }

        public int ItemCount { get; set; }
        public List<RmaRefundPaymentVM> RefundPayments { get; set; } = new();
    }

    public class RmaItemVM
    {
        public int RmaItemId { get; set; }
        public int ReturnRequestId { get; set; }
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }

        public int Qty { get; set; }
        public int? ApprovedQty { get; set; }
        public int? RefundQty { get; set; }
        public int? ReshipQty { get; set; }
        public decimal? RefundUnitAmount { get; set; }
        public decimal Subtotal => (RefundUnitAmount ?? 0m) * Qty;

        public DateTime CreatedDate { get; set; }
        public DateTime? RevisedDate { get; set; }

        // 顯示補充（若之後有 join 商品）
        public string? ProductName { get; set; }
        public string? SkuSpec { get; set; }
    }

    public class RmaRefundPaymentVM
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public int? ReturnRequestId { get; set; }
        public int PaymentConfigId { get; set; }
        public int Amount { get; set; }

        public string? Status { get; set; }           // 04
        public string? StatusName { get; set; }       // 04 中文

        public string? TradeNo { get; set; }
        public string? MerchantTradeNo { get; set; }
        public DateTime? TradeDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class RmaDecisionVM
    {
        public int RequestId { get; set; }
        public bool Approve { get; set; }
        public string? Note { get; set; }
    }
}
