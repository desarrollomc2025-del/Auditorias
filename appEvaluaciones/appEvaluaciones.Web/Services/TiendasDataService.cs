using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Services;

public sealed class TiendasDataService(ISqlConnectionFactory factory) : ITiendasService
{
    public async Task<IReadOnlyList<Tienda>> GetAllAsync(CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        var rows = await db.QueryAsync<Tienda>(new CommandDefinition(
            "SELECT TiendaId, EmpresaId, Codigo, CodigoInterno, Descripcion, Latitud, Longitud, Eliminado, FechaCreacion FROM dbo.Tiendas WHERE Eliminado = 0",
            cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Tienda?> GetByIdAsync(int tiendaId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        return await db.QuerySingleOrDefaultAsync<Tienda>(new CommandDefinition(
            "SELECT TiendaId, EmpresaId, Codigo, CodigoInterno, Descripcion, Latitud, Longitud, Eliminado, FechaCreacion FROM dbo.Tiendas WHERE TiendaId = @tiendaId",
            new { tiendaId }, cancellationToken: ct));
    }

    public async Task<int> UpsertAsync(Tienda tienda, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Tiendas WHERE TiendaId=@TiendaId)
BEGIN
    UPDATE dbo.Tiendas SET EmpresaId=@EmpresaId, Codigo=@Codigo, CodigoInterno=@CodigoInterno, Descripcion=@Descripcion,
           Latitud=@Latitud, Longitud=@Longitud, Eliminado=@Eliminado
    WHERE TiendaId=@TiendaId;
    SELECT @TiendaId;
END
ELSE
BEGIN
    INSERT INTO dbo.Tiendas(EmpresaId, Codigo, CodigoInterno, Descripcion, Latitud, Longitud, Eliminado, FechaCreacion)
    VALUES(@EmpresaId, @Codigo, @CodigoInterno, @Descripcion, @Latitud, @Longitud, @Eliminado, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, tienda, cancellationToken: ct));
    }

    public async Task DeleteAsync(int tiendaId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Tiendas SET Eliminado = 1 WHERE TiendaId = @tiendaId",
            new { tiendaId }, cancellationToken: ct));
    }
}

