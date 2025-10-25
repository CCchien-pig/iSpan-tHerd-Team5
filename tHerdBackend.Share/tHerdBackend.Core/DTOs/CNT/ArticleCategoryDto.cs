using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.CNT
{
	public class ArticleCategoryDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public int ArticleCount { get; set; } = 0;   // 每個分類的文章數量
	}
}

