using System.Data;
using Microsoft.Data.Sqlite;

namespace Drg.Data;

public enum DbProvider { Sqlite, SqlServer, Oracle }

public interface IDbConnectionFactory
{
    IDbConnection Create();
}

/// <summary>
/// Provider-neutral 連線工廠。連線字串一律由 <c>DrgOptions</c>(環境變數)提供,絕不內嵌(憲章原則 III)。
/// </summary>
public sealed class DbConnectionFactory(DbProvider provider, string connectionString) : IDbConnectionFactory
{
    public IDbConnection Create() => provider switch
    {
        DbProvider.Sqlite => new SqliteConnection(connectionString),
        // SqlServer / Oracle:加入對應 provider 套件(Microsoft.Data.SqlClient / Oracle.ManagedDataAccess)後接上,
        // 不影響分組結果(附加限制:DB 中立)。
        _ => throw new NotSupportedException($"DB provider '{provider}' 尚未接上;請加入對應 provider 套件。")
    };
}
