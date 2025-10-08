using tHerdBackend.Core.DTOs.SYS;

namespace tHerdBackend.Core.Interfaces.SYS
{
    public interface ISysCodeRepository
    {
        Task<IEnumerable<SysCodeDto>> GetSysCodes(string ModuleId, List<string> CodeIds);
    }
}
