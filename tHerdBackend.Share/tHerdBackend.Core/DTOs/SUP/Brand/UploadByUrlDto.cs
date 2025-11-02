using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	public class UploadByUrlDto
	{
		[Required]
		public string ImageUrl { get; set; }
		public string BlockType { get; set; }
		public string AltText { get; set; }
		public string Caption { get; set; }
		public bool IsActive { get; set; }
	}
}
