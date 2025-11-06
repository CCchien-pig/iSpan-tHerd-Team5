namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	public class BrandOverviewDto
	{
		public int ProductCount { get; set; }
		public int FavoriteCount { get; set; }
		//public DateTime CreatedDate { get; set; }

		public int CreatedDaysAgo { get; set; } // 新增：距今天數
		public string SupplierName { get; set; }
		public int TotalSalesQty { get; set; }

	}

}
