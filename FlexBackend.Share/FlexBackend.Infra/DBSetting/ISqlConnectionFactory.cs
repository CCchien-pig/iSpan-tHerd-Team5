using System.Data;

namespace FlexBackend.Infra.DBSetting
{
    public interface ISqlConnectionFactory
    {
        IDbConnection Create();
    }
}
