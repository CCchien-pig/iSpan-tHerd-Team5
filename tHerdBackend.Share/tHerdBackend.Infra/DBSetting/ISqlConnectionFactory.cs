using System.Data;

namespace tHerdBackend.Infra.DBSetting
{
    public interface ISqlConnectionFactory
    {
        IDbConnection Create();
    }
}
