using FlexBackend.Core.Models;

namespace FlexBackend.Core.DTOs.SYS
{
    /// <summary>
    /// 菜單 - 程式設定
    /// </summary>
    public class SysProgramConfigDto : SysProgramConfig
    {
        // 模組名稱
        public string ModuleName { get; set; }
    }

    /// <summary>
    /// 菜單主檔
    /// </summary>
    public class MenuModuleDto
    {
        public string ModuleId { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-fw fa-folder";
        public List<MenuItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// 菜單明細
    /// </summary>
    public class MenuItemDto
    {
        public string ProgId { get; set; } = string.Empty;
        public string ProgName { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? Controller { get; set; }
        public string? ActionName { get; set; }
        public int OrderSeq { get; set; }
        public string Icon { get; set; } = string.Empty;
    }
}
