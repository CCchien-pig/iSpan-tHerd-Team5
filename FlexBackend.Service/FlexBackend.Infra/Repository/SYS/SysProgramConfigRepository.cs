using Dapper;
using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Core.Models;
using FlexBackend.Infra.DBSetting;

namespace FlexBackend.Infra.Repository.SYS
{
    public class SysProgramConfigRepository : ISysProgramConfigRepository
    {
        private readonly ISqlConnectionFactory _factory;
        public SysProgramConfigRepository(ISqlConnectionFactory factory) => _factory = factory;

        public IEnumerable<SysProgramConfig> GetAll()
        {
            using var db = _factory.Create();
            return db.Query<SysProgramConfig>("SELECT * FROM SYS_ProgramConfig");
        }

        public Task<IEnumerable<SysProgramConfig>> GetByModuleAsync(string moduleId)
        {
            throw new NotImplementedException();
        }
    }
}
