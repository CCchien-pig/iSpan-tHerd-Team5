using tHerdBackend.Share.DTOs.CNT;

namespace tHerdBackend.Core.DTOs.CNT
{
	public class ArticleDetailResponse
	{
		/// <summary>這次呼叫是否可以看到全文</summary>
		public bool CanViewFullContent { get; set; }

		/// <summary>文章本體（標題、Blocks 等）</summary>
		public ArticleDetailDto? Data { get; set; }

		/// <summary>推薦文章（你原本就有的列表，如果暫時不做可以先給空陣列）</summary>
		public List<ArticleListDto> Recommended { get; set; } = new();
	}
}
