using FlexBackend.Core.DTOs.SYS;

namespace FlexBackend.Core.Interfaces.SYS
{
    public interface ISysProgramConfigRepository
    {
        Task<IEnumerable<MenuModuleDto>> GetSidebarAsync(bool onlyActive = true);
    }
}
