using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.UIKit.Rcl.ViewComponents
{
	public class FolderTreeSelectViewComponent : ViewComponent
	{
		private readonly tHerdDBContext _db;
		public FolderTreeSelectViewComponent(tHerdDBContext db) => _db = db;

		public IViewComponentResult Invoke(string inputId = "ParentFolderId", int? selectedId = null)
		{
			ViewData["InputId"] = inputId;
			ViewData["SelectedId"] = selectedId;
			return View();
		}
	}
}
