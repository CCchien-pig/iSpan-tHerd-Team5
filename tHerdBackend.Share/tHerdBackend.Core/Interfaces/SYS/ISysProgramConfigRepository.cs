using tHerdBackend.Core.DTOs.SYS;

namespace tHerdBackend.Core.Interfaces.SYS
{
    public interface ISysProgramConfigRepository
    {
        Task<IEnumerable<MenuModuleDto>> GetSidebarAsync(bool onlyActive = true);
    }
}
