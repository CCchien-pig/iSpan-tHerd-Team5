using FlexBackend.Core.DTOs.SYS;

namespace FlexBackend.Core.Interfaces.Share
{
    public interface IMenuService
    {
        Task<List<SysProgramConfigDto>> GetSidebarAsync(bool onlyActive = true);
    }
}
