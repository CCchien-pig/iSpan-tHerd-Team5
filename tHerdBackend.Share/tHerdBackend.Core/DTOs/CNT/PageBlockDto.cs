using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Share.DTOs.CNT
{
	/// <summary>
	/// CNT 文章內容區塊 DTO - 對應 CNT_PageBlock
	/// </summary>
	public class PageBlockDto
	{
		/// <summary>
		/// 區塊排序順序（根據後台設定）
		/// </summary>
		public int Order { get; set; }

		/// <summary>
		/// 區塊類型（如：heading / paragraph / image / list / quote...）
		/// 用於判斷前端要渲染哪一種元件
		/// </summary>
		public required string BlockType { get; set; }

		/// <summary>
		/// 區塊主要內容
		/// - 標題：文字
		/// - 內文：HTML 或文字
		/// - 圖片：圖片路徑 / URL
		/// </summary>
		public required string Content { get; set; }

		/// <summary>
		/// 其他附加資料（JSON 格式）
		/// 用於圖片屬性（alt、caption）或清單項目等
		/// 可為 null
		/// </summary>
		public string? ExtraJson { get; set; }
	}
}

