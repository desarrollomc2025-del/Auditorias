using System.Data;
using Dapper;
using appEvaluaciones.Shared.Services;
using appEvaluaciones.Web.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class EvaluacionesEndpoints
{
    private sealed record CreateEvaluacionDto(Guid EvaluacionKey, int TiendaId);
    private sealed record DetalleDto(int PreguntaId, bool? Respuesta, string? Comentario, decimal Ponderacion);
    private sealed record DetalleVm(int PreguntaId, bool? Respuesta, string? Comentario, decimal Ponderacion);
    private sealed record EvaluacionVm(int EvaluacionId, Guid EvaluacionKey, int TiendaId, DateTime FechaCreacion, IReadOnlyList<DetalleVm> Detalles);

    public static RouteGroupBuilder MapEvaluaciones(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/evaluaciones");

        group.MapGet("/{key:guid}", async (Guid key, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"SELECT TOP 1 EvaluacionId, EvaluacionKey, TiendaId, FechaCreacion
FROM dbo.Evaluaciones WHERE EvaluacionKey=@key;
SELECT PreguntaId, Respuesta, Comentario, Ponderacion
FROM dbo.DetalleEvaluaciones d
JOIN dbo.Evaluaciones e ON e.EvaluacionId = d.EvaluacionId
WHERE e.EvaluacionKey=@key
ORDER BY PreguntaId;";
            using var gr = await db.QueryMultipleAsync(new CommandDefinition(sql, new { key }, cancellationToken: ct, commandTimeout: 60));
            var head = await gr.ReadFirstOrDefaultAsync<(int EvaluacionId, Guid EvaluacionKey, int TiendaId, DateTime FechaCreacion)>();
            if (head.Equals(default((int, Guid, int, DateTime))))
                return Results.NotFound();
            var detalles = (await gr.ReadAsync<DetalleVm>()).ToList();
            var vm = new EvaluacionVm(head.EvaluacionId, head.EvaluacionKey, head.TiendaId, head.FechaCreacion, detalles);
            return Results.Ok(vm);
        });

        group.MapPost("", async (CreateEvaluacionDto dto, IEvaluacionesService svc, CancellationToken ct) =>
        {
            var id = await svc.CreateAsync(dto.EvaluacionKey, dto.TiendaId, ct);
            return Results.Ok(id);
        });

        group.MapPost("/{key:guid}/detalle", async (Guid key, DetalleDto detalle, IEvaluacionesService svc, CancellationToken ct) =>
        {
            await svc.UpsertDetalleAsync(key, detalle.PreguntaId, detalle.Respuesta, detalle.Comentario, detalle.Ponderacion, ct);
            return Results.NoContent();
        });

        return group;
    }
}
