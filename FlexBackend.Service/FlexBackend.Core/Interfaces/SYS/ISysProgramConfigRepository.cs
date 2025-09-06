using FlexBackend.Core.DTOs.SYS;
using FlexBackend.Core.Models;

namespace FlexBackend.Core.Interfaces.SYS
{
    public interface ISysProgramConfigRepository
    {
        Task<IEnumerable<MenuModuleDto>> GetSidebarAsync(bool onlyActive = true);
    }
}
