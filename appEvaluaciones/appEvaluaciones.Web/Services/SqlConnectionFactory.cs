using System.Data;
using Microsoft.Data.SqlClient;

namespace appEvaluaciones.Web.Services;

public interface ISqlConnectionFactory
{
    IDbConnection Create();
}

public sealed class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    private readonly string? _cs = configuration.GetConnectionString("Auditorias");

    public IDbConnection Create()
    {
        if (string.IsNullOrWhiteSpace(_cs))
            throw new InvalidOperationException("ConnectionStrings:Auditorias not configured");
        return new SqlConnection(_cs);
    }
}

