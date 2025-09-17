using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class TinyMceInitVM
	{
		public string Selector { get; set; } = "#editor";
		public int Height { get; set; } = 600;
		public string ImageUploadUrl { get; set; } = "/upload/image";
		public string MediaUploadUrl { get; set; } = "/upload/media";
		public bool EnablePreview { get; set; } = true;
		public bool EnableCounter { get; set; } = true;
	}
}
