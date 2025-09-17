using FlexBackend.Core.DTOs.SYS;

namespace FlexBackend.Core.Interfaces.SYS
{
    public interface ISysCodeRepository
    {
        Task<IEnumerable<SysCodeDto>> GetSysCodes(string ModuleId, List<string> CodeIds);
    }
}
