using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Services;

public sealed class TiposTiendaDataService(ISqlConnectionFactory factory) : ITiposTiendaService
{
    public async Task<IReadOnlyList<TipoTienda>> GetAllAsync(CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        var rows = await db.QueryAsync<TipoTienda>(new CommandDefinition(
            "SELECT TipoTiendaId, Nombre, Descripcion, Activo FROM dbo.TiposTienda WHERE Activo = 1",
            cancellationToken: ct));
        return rows.ToList();
    }
}

