using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.PROD;

namespace tHerdBackend.Core.Interfaces.CNT
{
	// 新功能：標籤→商品列表頁要用的服務
	// Controller 會呼叫這個，拿到 PagedResult<ProdProductDto>
	/// <summary>
	/// 給 Controller 用的服務介面：
	/// 輸入 tagId / page / pageSize，回傳商品卡片頁面要顯示的分頁商品資料
	/// </summary>
	public interface ITagProductQueryService
	{
		Task<PagedResult<ProdProductDto>> GetProductsByTagAsync(
			int tagId,
			int page,
			int pageSize,
			string sort   // ⭐ 新增
		);
	}
}
