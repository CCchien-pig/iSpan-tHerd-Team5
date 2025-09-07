using FlexBackend.Core.Interfaces.SYS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

public class PageTitleViewComponent : ViewComponent
{
    private readonly ISysProgramConfigRepository _menu; 
    public PageTitleViewComponent(ISysProgramConfigRepository menu) => _menu = menu;

    /// <summary>
    /// 取得頁面名稱
    /// </summary>
    /// <returns></returns>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var area = RouteData.Values["area"]?.ToString()
                   ?? HttpContext.GetRouteValue("area")?.ToString() ?? "";
        var controller = RouteData.Values["controller"]?.ToString() ?? "";
        var action = RouteData.Values["action"]?.ToString() ?? "Index";

        // 先 await 取得模組清單
        var modules = await _menu.GetSidebarAsync();

        // 在 Items 裡找出目前的程式
        var current = modules
            .SelectMany(m => m.Items)
            .FirstOrDefault(i =>
                string.Equals(i.Area ?? "", area, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.Controller ?? "", controller, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.ActionName ?? "", action, StringComparison.OrdinalIgnoreCase));

        var title = current?.ProgName ?? "";

        return Content(title);
    }
}
