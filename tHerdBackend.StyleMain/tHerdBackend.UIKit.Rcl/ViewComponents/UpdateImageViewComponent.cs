using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs;

namespace tHerdBackend.UIKit.Rcl.ViewComponents.UpdateImage
{
	public class UpdateImageViewComponent : ViewComponent
	{
		public IViewComponentResult Invoke(
			string title = "圖片資訊",
			SysAssetFileDto? file = null
        )
		{
			var vm = new UpdateImageViewModel
			{
				Title = title,
				File = file ?? new SysAssetFileDto()
			};

			return View("Default", vm);
		}
	}

	public class UpdateImageViewModel
	{
		public string Title { get; set; } = "";
		public SysAssetFileDto File { get; set; } = new();
	}
}
