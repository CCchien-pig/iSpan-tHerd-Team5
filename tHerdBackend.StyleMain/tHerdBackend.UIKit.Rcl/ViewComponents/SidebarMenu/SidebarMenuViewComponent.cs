using tHerdBackend.Core.DTOs.SYS;
using tHerdBackend.Core.DTOs.USER;               // ApplicationUser, ApplicationRole
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Infra.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


//namespace tHerdBackend.UIKit.Rcl.ViewComponents.SidebarMenu
//{
//    public class SidebarMenuViewComponent : ViewComponent
//    {
//        private readonly ISysProgramConfigRepository _menu;
//        public SidebarMenuViewComponent(ISysProgramConfigRepository menu) => _menu = menu;


//        public async Task<IViewComponentResult> InvokeAsync()
//        {
//            var modules = await _menu.GetSidebarAsync();


//            // 取得目前路由，供 view 樣式判斷 active/show
//            var area = (string?)RouteData.Values["area"] ?? HttpContext.GetRouteValue("area")?.ToString();
//            var controller = (string?)RouteData.Values["controller"] ?? HttpContext.GetRouteValue("controller")?.ToString();
//            var action = (string?)RouteData.Values["action"] ?? HttpContext.GetRouteValue("action")?.ToString();

//            ViewData["CurrentArea"] = area;
//            ViewData["CurrentController"] = controller;
//            ViewData["CurrentAction"] = action;

//            return View(modules);
//        }
//    }
//}

namespace tHerdBackend.UIKit.Rcl.ViewComponents.SidebarMenu
{
	public class SidebarMenuViewComponent : ViewComponent
	{
		private readonly ISysProgramConfigRepository _menu;
		private readonly tHerdDBContext _db;        // 讀 USER_RoleModule
		private readonly ApplicationDbContext _app; // 讀 AspNetUserRoles

		public SidebarMenuViewComponent(
			ISysProgramConfigRepository menu,
			tHerdDBContext db,
			ApplicationDbContext app)
		{
			_menu = menu;
			_db = db;
			_app = app;
		}

		public async Task<IViewComponentResult> InvokeAsync()
		{
			// 1) 目前使用者
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				setRouteToViewData();
				return View("Default", Enumerable.Empty<MenuModuleDto>());
			}

			// 2) 取使用者 RoleId 清單（從 AspNetUserRoles）
			var roleIds = await _app.UserRoles
				.Where(ur => ur.UserId == userId)
				.Select(ur => ur.RoleId)
				.ToListAsync();

			if (roleIds.Count == 0)
			{
				setRouteToViewData();
				return View("Default", Enumerable.Empty<MenuModuleDto>());
			}

			// 3) 依角色查可見模組代號（USER_RoleModule.ModuleId）
			var allowedModules = await _db.UserRoleModules
				.Where(rm => roleIds.Contains(rm.AdminRoleId))
				.Select(rm => rm.ModuleId)
				.Distinct()
				.ToListAsync();

			if (allowedModules.Count == 0)
			{
				setRouteToViewData();
				return View("Default", Enumerable.Empty<MenuModuleDto>());
			}

			// 4) 取原本的選單資料（你的 Repo），然後做過濾
			//    ★ 若你願意，也可在 Repo 內加一個 GetSidebarAsync(IEnumerable<string> moduleIds)
			//      直接用 SQL 過濾，效率更好。這裡先在記憶體過濾即可。
			var modules = await _menu.GetSidebarAsync(); // IEnumerable<MenuModuleDto>

			var filtered = modules.Where(m => !string.IsNullOrWhiteSpace(m.ModuleId)
							   && allowedModules.Contains(m.ModuleId!))
								.ToList();

			// 5) 丟目前路由資訊給 View（你的 Default.cshtml 會用來判斷 active/show）
			setRouteToViewData();

			return View("Default", filtered);
		}

		private void setRouteToViewData()
		{
			ViewData["CurrentArea"] = (string?)RouteData.Values["area"]
				?? HttpContext.GetRouteValue("area")?.ToString() ?? string.Empty;
			ViewData["CurrentController"] = (string?)RouteData.Values["controller"]
				?? HttpContext.GetRouteValue("controller")?.ToString() ?? string.Empty;
			ViewData["CurrentAction"] = (string?)RouteData.Values["action"]
				?? HttpContext.GetRouteValue("action")?.ToString() ?? string.Empty;
		}
	}
}
