using Dapper;
using FlexBackend.Core.DTOs.SYS;
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

        public async Task<IEnumerable<MenuModuleDto>> GetSidebarAsync(bool onlyActive = true)
        {
            using var db = _factory.Create();


            // 1) 讀出所有功能（排序後）
            var sql = @"
                SELECT pc.ModuleId, pc.ProgId, pc.ProgName, pc.Icon, pc.Area, pc.Controller, pc.ActionName, pc.OrderSeq,
                sc.CodeDesc AS ModuleName
                FROM SYS_ProgramConfig pc
                LEFT JOIN SYS_Code sc
                ON sc.ModuleId = 'SYS' AND sc.CodeId = '01' AND sc.CodeNo = pc.ModuleId
                WHERE pc.IsActive = 1
                ORDER BY sc.GroupName, pc.ModuleId, pc.OrderSeq, pc.ProgId;";


            var rows = await db.QueryAsync<dynamic>(sql);


            // 2) 分組組成模組 → 子項目
            var modules = new Dictionary<string, MenuModuleDto>(StringComparer.OrdinalIgnoreCase);


            foreach (var r in rows)
            {
                string moduleId = r.ModuleId;
                if (!modules.TryGetValue(moduleId, out var module))
                {
                    module = new MenuModuleDto
                    {
                        ModuleId = moduleId,
                        ModuleName = (string?)r.ModuleName ?? moduleId,
                        Icon = string.IsNullOrWhiteSpace((string?)r.Icon) ? "fas fa-fw fa-folder" : (string)r.Icon
                    };
                    modules.Add(moduleId, module);
                }


                module.Items.Add(new MenuItemDto
                {
                    ProgId = r.ProgId,
                    ProgName = r.ProgName,
                    Area = r.Area,
                    Controller = r.Controller,
                    ActionName = r.ActionName,
                    OrderSeq = (int)r.OrderSeq,
                    Icon = (string?)r.Icon ?? string.Empty
                });
            }


            // 模組圖示：若 module.Icon 是預設值，嘗試從第一個有 icon 的子項目帶入
            foreach (var m in modules.Values)
            {
                if (m.Icon == "fas fa-fw fa-folder")
                {
                    var firstIcon = m.Items.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.Icon))?.Icon;
                    if (!string.IsNullOrWhiteSpace(firstIcon)) m.Icon = firstIcon!;
                }
            }


            return modules.Values;
        }
    }
}
