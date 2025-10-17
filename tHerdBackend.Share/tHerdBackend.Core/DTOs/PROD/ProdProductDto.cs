using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品清單
    /// </summary>
    public class ProdProductDto : IProductUserInfo
	{
		public string BrandName { get; set; } = string.Empty;

		public int ProductId { get; set; } = 0;

		public List<string> ProductTypeDesc { get; set; } = new List<string>();

		public string ProductCode { get; set; } = string.Empty;

		public string ProductName { get; set; } = string.Empty;

		public string SupplierName { get; set; } = string.Empty;

		public bool IsPublished { get; set; } = false;

		public int Creator { get; set; }

		public string CreatorNm { get; set; } = string.Empty;

		public DateTime CreatedDate { get; set; } = DateTime.MinValue;

		public string FormateCreatedDate => DateTimeHelper.ToDateTimeString(CreatedDate);

		public int? Reviser { get; set; }

		public string ReviserNm { get; set; } = string.Empty;

		// =====
		public string Badge { get; set; } = string.Empty;
	}
}
