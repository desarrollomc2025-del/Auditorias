using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Web.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class TiposTiendaEndpoints
{
    public static RouteGroupBuilder MapTiposTienda(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tipos-tienda");

        group.MapGet("", async (ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.QueryAsync<TipoTienda>(new CommandDefinition(
                "SELECT TipoTiendaId, Nombre, Descripcion, Activo FROM dbo.TiposTienda WHERE Activo = 1",
                cancellationToken: ct));
            return Results.Ok(rows);
        });

        return group;
    }
}

