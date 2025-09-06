using FlexBackend.Core.Interfaces.SYS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FlexBackend.UIKit.Rcl.ViewComponents
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
