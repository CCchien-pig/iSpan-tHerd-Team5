using FlexBackend.Core.DTOs.SYS;
using FlexBackend.Core.Models;

namespace FlexBackend.Core.Interfaces.SYS
{
    public interface ISysProgramConfigRepository
    {
        IEnumerable<SysProgramConfig> GetAll();
        Task<IEnumerable<SysProgramConfig>> GetByModuleAsync(string moduleId);
        Task<IEnumerable<MenuModuleDto>> GetSidebarAsync(bool onlyActive = true);
    }
}
