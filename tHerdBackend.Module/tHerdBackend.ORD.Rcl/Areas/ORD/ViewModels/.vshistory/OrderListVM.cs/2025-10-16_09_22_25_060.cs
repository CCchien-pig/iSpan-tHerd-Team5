using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.ORD.Rcl.Areas.ORD.ViewModels
{
	/// <summary>
	/// 訂單列表 ViewModel
	/// </summary>
	public class OrderListVM
	{

		/// <summary>
		/// 訂單列表
		/// </summary>
		public List<OrderListItemVM> Orders { get; set; } = new List<OrderListItemVM>();

		/// <summary>
		/// 搜尋條件
		/// </summary>
		public OrderSearchVM SearchParams { get; set; } = new OrderSearchVM();


		/// <summary>
		/// 分頁資訊
		/// </summary>
		public PaginationVM Pagination { get; set; } = new PaginationVM();

		/// <summary>
		/// 排序選項
		/// </summary>
		public IEnumerable<SortOption> SortOptions { get; set; } = new List<SortOption>();

		/// <summary>
		/// 訂單狀態選項（供下拉選單使用）
		/// </summary>
		public IEnumerable<SelectOption> OrderStatusOptions { get; set; } = new List<SelectOption>();

		/// <summary>
		/// 配送狀態選項（供下拉選單使用）
		/// </summary>
		public IEnumerable<SelectOption> ShippingStatusOptions { get; set; } = new List<SelectOption>();
		/// <summary>
		/// 付款狀態選項（供下拉選單使用）
		/// </summary>
		public IEnumerable<SelectOption> PaymentStatusOptions { get; set; } = new List<SelectOption>();
	}

	/// <summary>
	/// 訂單列表項目 ViewModel
	/// </summary>
	public class OrderListItemVM
	{
		/// <summary>
		/// 訂單ID
		/// </summary>
		public int OrderId { get; set; }

		/// <summary>
		/// 訂單編號
		/// </summary>
		[Display(Name = "訂單編號")]
		public string OrderNo { get; set; }

		/// <summary>
		/// 會員編號
		/// </summary>
		public int UserNumberId { get; set; }

		/// <summary>
		/// 會員姓名
		/// </summary>
		[Display(Name = "會員姓名")]
		public string UserName { get; set; }

		/// <summary>
		/// 訂單狀態ID
		/// </summary>
		[Display(Name = "訂單狀態")]
		public string OrderStatusId { get; set; }

		/// <summary>
		/// 訂單狀態名稱
		/// </summary>
		public string OrderStatusName { get; set; }

		/// <summary>
		/// 付款狀態
		/// </summary>
		[Display(Name = "付款狀態")]
		public string PaymentStatus { get; set; }

		/// <summary>
		/// 付款狀態顯示名稱
		/// </summary>
		public string PaymentStatusName { get; set; }

		/// <summary>
		/// 配送狀態ID
		/// </summary>
		[Display(Name = "配送狀態")]
		public string ShippingStatusId { get; set; }

		/// <summary>
		/// 配送狀態名稱
		/// </summary>
		public string ShippingStatusName { get; set; }

		/// <summary>
		/// 小計金額
		/// </summary>
		[Display(Name = "小計")]
		[DisplayFormat(DataFormatString = "{0:C0}")]
		public decimal Subtotal { get; set; }

		/// <summary>
		/// 折扣總額
		/// </summary>
		[Display(Name = "折扣")]
		[DisplayFormat(DataFormatString = "{0:C0}")]
		public decimal DiscountTotal { get; set; }

		/// <summary>
		/// 運費
		/// </summary>
		[Display(Name = "運費")]
		[DisplayFormat(DataFormatString = "{0:C0}")]
		public decimal ShippingFee { get; set; }

		/// <summary>
		/// 訂單總金額（計算屬性）
		/// </summary>
		[Display(Name = "總金額")]
		[DisplayFormat(DataFormatString = "{0:C0}")]
		public decimal TotalAmount => Subtotal - DiscountTotal + ShippingFee;

		/// <summary>
		/// 收件人姓名
		/// </summary>
		[Display(Name = "收件人")]
		public string ReceiverName { get; set; }

		/// <summary>
		/// 收件人電話
		/// </summary>
		[Display(Name = "聯絡電話")]
		public string ReceiverPhone { get; set; }

		/// <summary>
		/// 建立時間
		/// </summary>
		[Display(Name = "建立時間")]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
		public DateTime CreatedDate { get; set; }

		/// <summary>
		/// 異動時間
		/// </summary>
		[Display(Name = "異動時間")]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
		public DateTime? RevisedDate { get; set; }

		/// <summary>
		/// 是否展開明細
		/// </summary>
		public bool IsExpanded { get; set; } = false;

		/// <summary>
		/// 訂單明細項目
		/// </summary>
		public List<OrderItemVM> OrderItems { get; set; } = new List<OrderItemVM>();

		/// <summary>
		/// 是否有出貨標籤
		/// </summary>
		public bool HasShippingLabel { get; set; }

		/// <summary>
		/// 物流商名稱
		/// </summary>
		public string LogisticsName { get; set; }

		/// <summary>
		/// 優惠券代碼
		/// </summary>
		public string CouponCode { get; set; }

		/// <summary>
		/// 付款狀態選項
		/// </summary>
		//public int OrderId { get; set; }
		//public string OrderNo { get; set; } = "";
		//public int UserNumberId { get; set; }
		//public string PaymentStatus { get; set; } = ""; // ORD/04
		//public string OrderStatusId { get; set; }          // ← 加這行
		//public string ShippingStatusId { get; set; }
		//public decimal Subtotal { get; set; }
		//public decimal DiscountTotal { get; set; }
		//public decimal ShippingFee { get; set; }
		//public DateTime CreatedDate { get; set; }
		//public bool IsVisibleToMember { get; set; }
		//public decimal Total => Subtotal - DiscountTotal + ShippingFee;

		public List<OrderItemVM> Items { get; set; } = new(); // 若有用展開明細

        public bool IsVisibleToMember { get; set; }

    }

    /// <summary>
    /// 訂單項目 ViewModel
    /// </summary>
    public class OrderItemVM
	{
		/// <summary>
		/// 訂單明細ID
		/// </summary>
		public int OrderId { get; set; }
		/// <summary>
		/// 訂單明細ID
		/// </summary>
		public int OrderItemId { get; set; }

		/// <summary>
		/// 商品ID
		/// </summary>
		public int ProductId { get; set; }

		/// <summary>
		/// SKU ID
		/// </summary>
		public int SkuId { get; set; }

		/// <summary>
		/// 商品名稱
		/// </summary>
		[Display(Name = "商品名稱")]
		public string ProductName { get; set; }

		/// <summary>
		/// SKU 規格
		/// </summary>
		[Display(Name = "規格")]
		public string SkuSpec { get; set; }

		/// <summary>
		/// 單價
		/// </summary>
		[Display(Name = "單價")]
		[DisplayFormat(DataFormatString = "{0:C0}")]
		public decimal UnitPrice { get; set; }

		/// <summary>
		/// 數量
		/// </summary>
		[Display(Name = "數量")]
		public int Qty { get; set; }

		/// <summary>
		/// 小計
		/// </summary>
		[Display(Name = "小計")]
		[DisplayFormat(DataFormatString = "{0:C0}")]
		public decimal Subtotal => UnitPrice * Qty;

		/// <summary>
		/// 商品圖片URL
		/// </summary>
		public string ProductImageUrl { get; set; }
	}

	/// <summary>
	/// 訂單搜尋條件 ViewModel
	/// </summary>
	public class OrderSearchVM
	{
		/// <summary>
		/// 關鍵字搜尋（訂單編號、會員姓名、收件人姓名、電話）
		/// </summary>
		[Display(Name = "搜尋")]
		public string Keyword { get; set; }

		/// <summary>
		/// 訂單狀態篩選
		/// </summary>
		[Display(Name = "訂單狀態")]
		public string OrderStatusId { get; set; }

		/// <summary>
		/// 付款狀態篩選
		/// </summary>
		[Display(Name = "付款狀態")]
		public string PaymentStatus { get; set; }

		/// <summary>
		/// 配送狀態篩選
		/// </summary>
		[Display(Name = "配送狀態")]
		public string ShippingStatusId { get; set; }

		/// <summary>
		/// 開始日期
		/// </summary>
		[Display(Name = "開始日期")]
		[DataType(DataType.Date)]
		public DateTime? StartDate { get; set; }

		/// <summary>
		/// 結束日期
		/// </summary>
		[Display(Name = "結束日期")]
		[DataType(DataType.Date)]
		public DateTime? EndDate { get; set; }

		/// <summary>
		/// 金額下限
		/// </summary>
		[Display(Name = "金額下限")]
		public decimal? MinAmount { get; set; }

		/// <summary>
		/// 金額上限
		/// </summary>
		[Display(Name = "金額上限")]
		public decimal? MaxAmount { get; set; }

		/// <summary>
		/// 排序欄位
		/// </summary>
		public string SortBy { get; set; } = "CreatedDate";

		/// <summary>
		/// 排序方向（ASC/DESC）
		/// </summary>
		public string SortDirection { get; set; } = "DESC";

		/// <summary>
		/// 目前頁碼
		/// </summary>
		public int PageNumber { get; set; } = 1;

		/// <summary>
		/// 每頁筆數
		/// </summary>
		public int PageSize { get; set; } = 10;

    }

	/// <summary>
	/// 分頁資訊 ViewModel
	/// </summary>
	public class PaginationVM
	{
		/// <summary>
		/// 目前頁碼
		/// </summary>
		public int CurrentPage { get; set; }

		/// <summary>
		/// 總頁數
		/// </summary>
		public int TotalPages { get; set; }

		/// <summary>
		/// 每頁筆數
		/// </summary>
		public int PageSize { get; set; }

		/// <summary>
		/// 總筆數
		/// </summary>
		public int TotalCount { get; set; }

		/// <summary>
		/// 是否有上一頁
		/// </summary>
		public bool HasPreviousPage => CurrentPage > 1;

		/// <summary>
		/// 是否有下一頁
		/// </summary>
		public bool HasNextPage => CurrentPage < TotalPages;

		/// <summary>
		/// 顯示的頁碼範圍
		/// </summary>
		public List<int> PageNumbers
		{
			get
			{
				var pages = new List<int>();
				var startPage = Math.Max(1, CurrentPage - 2);
				var endPage = Math.Min(TotalPages, CurrentPage + 2);

				for (int i = startPage; i <= endPage; i++)
				{
					pages.Add(i);
				}

				return pages;
			}
		}

		/// <summary>
		/// 每頁筆數選項
		/// </summary>
		public List<int> PageSizeOptions => new List<int> { 10, 20, 50, 100 };
	}

	/// <summary>
	/// 排序選項
	/// </summary>
	public class SortOption
	{
		public string Value { get; set; }
		public string Text { get; set; }
		public string Direction { get; set; }
	}

	/// <summary>
	/// 選項（供下拉選單使用）
	/// </summary>
	public class SelectOption
	{
		public string Value { get; set; }
		public string Text { get; set; }
	}

	/// <summary>
	/// 訂單狀態更新 ViewModel
	/// </summary>
	public class OrderStatusUpdateVM
	{
		/// <summary>
		/// 訂單ID
		/// </summary>
		[Required]
		public int OrderId { get; set; }

		/// <summary>
		/// 新的訂單狀態ID
		/// </summary>
		[Display(Name = "訂單狀態")]
		public string? OrderStatusId { get; set; }

		/// <summary>
		/// 新的配送狀態ID
		/// </summary>
		[Display(Name = "配送狀態")]
		public string? ShippingStatusId { get; set; }

		/// <summary>
		/// 備註
		/// </summary>
		[Display(Name = "備註")]
		public string Remarks { get; set; }
	}
}