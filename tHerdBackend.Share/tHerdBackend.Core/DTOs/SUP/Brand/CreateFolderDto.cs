using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	// 建議新增一個 DTO 來接收建立資料夾的請求
	public class CreateFolderDto
	{
		public string FolderName { get; set; }
		public int? ParentId { get; set; }
	}
}
