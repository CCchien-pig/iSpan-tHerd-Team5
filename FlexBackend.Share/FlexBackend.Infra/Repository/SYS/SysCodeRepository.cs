using Dapper;
using FlexBackend.Core.DTOs.SYS;
using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Infra.DBSetting;

namespace FlexBackend.Infra.Repository.SYS
{
    public class SysCodeRepository : ISysCodeRepository
    {
        private readonly ISqlConnectionFactory _factory;
        public SysCodeRepository(ISqlConnectionFactory factory) => _factory = factory;

        public async Task<IEnumerable<SysCodeDto>> GetSysCodes(string ModuleId, List<string> CodeIds)
        {
            using var db = _factory.Create();


            // 1) 讀出所有功能（排序後）
            var sql = @"SELECT CodeId, CodeNo, CodeDesc, Memo, GroupName, ProgId
                FROM SYS_Code
                WHERE ModuleId = @ModuleId AND CodeId IN @CodeIds AND IsActive = 1
                ORDER BY CodeId, CodeNo;";

            var rows = await db.QueryAsync<SysCodeDto>(sql, new { ModuleId, CodeIds });

            return rows;
        }
    }
}
