using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs;

namespace tHerdBackend.UIKit.Rcl.ViewComponents.UpdateImage
{
	public class UpdateImageViewComponent : ViewComponent
	{
		public IViewComponentResult Invoke(
			string modalId = "imgMetaModal",
			string title = "圖片資訊",
			SysAssetFileDto? file = null,
			string updateApiUrl = "/SYS/Images/UpdateFile",
			string deleteApiUrl = "/SYS/Images/DeleteFile"
        )
		{
			var vm = new UpdateImageViewModel
			{
				ModalId = modalId,
				Title = title,
				File = file ?? new SysAssetFileDto(),
				UpdateApiUrl = updateApiUrl,
				DeleteApiUrl = deleteApiUrl
			};

			return View("Default", vm);
		}
	}

	public class UpdateImageViewModel
	{
		public string ModalId { get; set; } = "";
		public string Title { get; set; } = "";
		public SysAssetFileDto File { get; set; } = new();
		public string UpdateApiUrl { get; set; }
		public string DeleteApiUrl { get; set; }
	}
}
