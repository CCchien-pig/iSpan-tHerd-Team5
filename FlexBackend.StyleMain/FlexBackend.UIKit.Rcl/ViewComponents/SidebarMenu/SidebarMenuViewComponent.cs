using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.UIKit.Rcl.ViewComponents.SidebarMenu
{
    public class SidebarMenuViewComponent : ViewComponent
    {
        private readonly ISysProgramConfigRepository _menu;
        public SidebarMenuViewComponent(ISysProgramConfigRepository menu) => _menu = menu;


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var modules = await _menu.GetSidebarAsync();


            // 取得目前路由，供 view 樣式判斷 active/show
            var area = (string?)RouteData.Values["area"] ?? HttpContext.GetRouteValue("area")?.ToString();
            var controller = (string?)RouteData.Values["controller"] ?? HttpContext.GetRouteValue("controller")?.ToString();
            var action = (string?)RouteData.Values["action"] ?? HttpContext.GetRouteValue("action")?.ToString();

            ViewData["CurrentArea"] = area;
            ViewData["CurrentController"] = controller;
            ViewData["CurrentAction"] = action;

            return View(modules);
        }
    }
}

//namespace FlexBackend.UIKit.Rcl.ViewComponents.SidebarMenu
//{
//	public class SidebarMenuViewComponent : ViewComponent
//	{
//		private readonly ISysProgramConfigRepository _menu;
//		private readonly tHerdDBContext _db;

//		public SidebarMenuViewComponent(ISysProgramConfigRepository menu, tHerdDBContext db)
//		{
//			_menu = menu; _db = db;
//		}

//		public async Task<IViewComponentResult> InvokeAsync()
//		{
//			// 1) 取當前使用者角色「名稱」(來自 Claims)
//			var roleNames = HttpContext.User?.Claims
//				.Where(c => c.Type == ClaimTypes.Role)
//				.Select(c => c.Value)
//				.Distinct()
//				.ToList() ?? new List<string>();

//			if (roleNames.Count == 0)
//				return View(new List<object>());

//			// 2) 名稱 → RoleId（用 tHerdDBContext 的 AspNetRoles）
//			var roleIds = await _db.RolesRef
//								.AsNoTracking()
//								.Where(r => roleNames.Contains(r.Name!))
//								.Select(r => r.Id)
//								.ToListAsync();

//			// 3) 角色 → 模組代號
//			var allowedModules = await _db.UserRoleModules
//				.Where(x => roleIds.Contains(x.AdminRoleId))
//				.Select(x => x.ModuleId)
//				.Distinct()
//				.ToListAsync();

//			if (allowedModules.Count == 0)
//				return View(new List<object>());

//			// 4) 取既有選單模型 → 過濾（假設 VM 有 ModuleCode 屬性）
//			var modules = await _menu.GetSidebarAsync();

//			// 建議改成強型別 m.ModuleCode；若你目前是匿名/不同型別，暫用反射存取
//			var filtered = modules.Where(m =>
//			{
//				var prop = m.GetType().GetProperty("ModuleCode");
//				var code = prop?.GetValue(m)?.ToString();
//				return !string.IsNullOrEmpty(code) && allowedModules.Contains(code!);
//			}).ToList();

//			// 5) 當前路由給 view 做 active 樣式
//			ViewData["CurrentArea"] = (string?)RouteData.Values["area"] ?? HttpContext.GetRouteValue("area")?.ToString();
//			ViewData["CurrentController"] = (string?)RouteData.Values["controller"] ?? HttpContext.GetRouteValue("controller")?.ToString();
//			ViewData["CurrentAction"] = (string?)RouteData.Values["action"] ?? HttpContext.GetRouteValue("action")?.ToString();

//			return View(filtered);
//		}
//	}
//}
