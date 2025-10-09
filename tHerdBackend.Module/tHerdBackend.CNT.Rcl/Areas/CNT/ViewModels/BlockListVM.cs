using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class BlockListVM
	{
		public int PageBlockId { get; set; }
		public string BlockType { get; set; }
		public string Content { get; set; }
		public int OrderSeq { get; set; }

		// 🟢 額外給 UI 用
		public int WordCount { get; set; }        // 該區塊字數
		public string PreviewText { get; set; }   // 前 100 字
	}

	//var blocks = page.CntPageBlocks
	//	.OrderBy(b => b.OrderSeq)
	//	.Select(b => new BlockListVM
	//	{
	//		PageBlockId = b.PageBlockId,
	//		BlockType = b.BlockType,
	//		Content = b.Content,
	//		OrderSeq = b.OrderSeq,
	//		WordCount = Regex.Replace(b.Content ?? "", "<.*?>", "").Trim().Length,
	//		PreviewText = Regex.Replace(b.Content ?? "", "<.*?>", "").Trim()
	//						   .Substring(0, Math.Min(100, b.Content?.Length ?? 0))
	//	}).ToList();

	//		var totalWordCount = blocks
	//			.Where(b => b.BlockType == "richtext")
	//			.Sum(b => b.WordCount);
	//這樣 totalWordCount 就是整篇文章的純文字字數。

//	前台「只顯示前 200 字」

//只要在 Controller 把所有 RichText block 的內容串起來再取前 200 字就行：

//var articleText = string.Join("",
//	blocks.Where(b => b.BlockType == "richtext")
//		  .Select(b => Regex.Replace(b.Content ?? "", "<.*?>", ""))
//);

//	var preview200 = articleText.Substring(0, Math.Min(200, articleText.Length));


//	這個 preview200 可以存在一個額外的 VM 屬性（例如 ArticlePreviewText）。


	}
