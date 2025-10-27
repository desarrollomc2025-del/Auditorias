using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Web.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class TiendasEndpoints
{
    public static RouteGroupBuilder MapTiendas(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tiendas");

        group.MapGet("", async (ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.QueryAsync<Tienda>(new CommandDefinition(
                "SELECT TiendaId, EmpresaId, Codigo, CodigoInterno, Descripcion, TipoTiendaId, Latitud, Longitud, Eliminado, FechaCreacion FROM dbo.Tiendas WHERE Eliminado = 0",
                cancellationToken: ct));
            return Results.Ok(rows);
        });

        group.MapGet("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var tienda = await db.QuerySingleOrDefaultAsync<Tienda>(new CommandDefinition(
                "SELECT TiendaId, EmpresaId, Codigo, CodigoInterno, Descripcion, TipoTiendaId, Latitud, Longitud, Eliminado, FechaCreacion FROM dbo.Tiendas WHERE TiendaId = @id",
                new { id }, cancellationToken: ct));
            return tienda is null ? Results.NotFound() : Results.Ok(tienda);
        });

        group.MapPost("", async (Tienda tienda, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Tiendas WHERE TiendaId=@TiendaId)
BEGIN
    UPDATE dbo.Tiendas SET EmpresaId=@EmpresaId, Codigo=@Codigo, CodigoInterno=@CodigoInterno, Descripcion=@Descripcion,
           TipoTiendaId=@TipoTiendaId, Latitud=@Latitud, Longitud=@Longitud, Eliminado=@Eliminado
    WHERE TiendaId=@TiendaId;
    SELECT @TiendaId;
END
ELSE
BEGIN
    INSERT INTO dbo.Tiendas(EmpresaId, Codigo, CodigoInterno, Descripcion, TipoTiendaId, Latitud, Longitud, Eliminado, FechaCreacion)
    VALUES(@EmpresaId, @Codigo, @CodigoInterno, @Descripcion, @TipoTiendaId, @Latitud, @Longitud, @Eliminado, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
            var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, tienda, cancellationToken: ct));
            return Results.Ok(id);
        });

        // Soft delete: marca Eliminado=1 (evita errores por FKs)
        group.MapDelete("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.Tiendas SET Eliminado = 1 WHERE TiendaId = @id",
                new { id }, cancellationToken: ct));
            return rows == 0 ? Results.NotFound() : Results.NoContent();
        });

        return group;
    }
}
