using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.CNT
{
	public class TagInfoDto
	{
		public int TagId { get; set; }
		public string TagName { get; set; } = string.Empty;
		public string? TagTypeName { get; set; }
		public string? Description { get; set; }
	}
}
