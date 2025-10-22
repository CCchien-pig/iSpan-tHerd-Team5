using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs;

namespace tHerdBackend.UIKit.Rcl.ViewComponents.UpdateImage
{
	public class UpdateImageViewComponent : ViewComponent
	{
		public IViewComponentResult Invoke(string modalId = "imgMetaModal", string title = "圖片資訊", SysAssetFileDto? file = null)
		{
			var vm = new UpdateImageViewModel
			{
				ModalId = modalId,
				Title = title,
				File = file ?? new SysAssetFileDto()
			};

			return View(vm);
		}
	}
	public class UpdateImageViewModel
	{
		public string ModalId { get; set; } = "imgMetaModal";
		public string Title { get; set; } = "圖片資訊";
		public SysAssetFileDto File { get; set; } = new();
	}
}
