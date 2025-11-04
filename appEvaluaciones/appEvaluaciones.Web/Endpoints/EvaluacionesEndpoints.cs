using System.Data;
using Dapper;
using appEvaluaciones.Shared.Services;
using appEvaluaciones.Web.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class EvaluacionesEndpoints
{
    private sealed record CreateEvaluacionDto(int TiendaId);
    private sealed record DetalleDto(int PreguntaId, bool? Respuesta, string? Comentario, decimal Ponderacion);
    private sealed record DetalleVm(int PreguntaId, bool? Respuesta, string? Comentario, decimal Ponderacion);
    private sealed record EvaluacionVm(int EvaluacionId, int TiendaId, DateTime FechaCreacion, IReadOnlyList<DetalleVm> Detalles);

    public static RouteGroupBuilder MapEvaluaciones(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/evaluaciones");

        group.MapGet("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"SELECT TOP 1 EvaluacionId, TiendaId, FechaCreacion
FROM dbo.Evaluaciones WHERE EvaluacionId=@id;
SELECT PreguntaId, Respuesta, Comentario, Ponderacion
FROM dbo.DetalleEvaluaciones d
JOIN dbo.Evaluaciones e ON e.EvaluacionId = d.EvaluacionId
WHERE e.EvaluacionId=@id
ORDER BY PreguntaId;";
            using var gr = await db.QueryMultipleAsync(new CommandDefinition(sql, new { id }, cancellationToken: ct, commandTimeout: 60));
            var head = await gr.ReadFirstOrDefaultAsync<(int EvaluacionId, int TiendaId, DateTime FechaCreacion)>();
            if (head.Equals(default((int, int, DateTime))))
                return Results.NotFound();
            var detalles = (await gr.ReadAsync<DetalleVm>()).ToList();
            var vm = new EvaluacionVm(head.EvaluacionId, head.TiendaId, head.FechaCreacion, detalles);
            return Results.Ok(vm);
        });

        group.MapPost("", async (CreateEvaluacionDto dto, IEvaluacionesService svc, HttpContext http, CancellationToken ct) =>
        {
            int? evaluadorId = null;
            var evalidClaim = http.User.Claims.FirstOrDefault(c => c.Type == "evalid")?.Value;
            if (int.TryParse(evalidClaim, out var parsed)) evaluadorId = parsed;
            var id = await svc.CreateAsync(dto.TiendaId, evaluadorId, ct);
            return Results.Ok(id);
        });

        group.MapPost("/{id:int}/detalle", async (int id, DetalleDto detalle, IEvaluacionesService svc, CancellationToken ct) =>
        {
            await svc.UpsertDetalleAsync(id, detalle.PreguntaId, detalle.Respuesta, detalle.Comentario, detalle.Ponderacion, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:int}/detalles", async (int id, List<DetalleDto> detalles, IEvaluacionesService svc, CancellationToken ct) =>
        {
            var items = detalles.Select(d => new appEvaluaciones.Shared.Services.DetalleUpsert
            {
                PreguntaId = d.PreguntaId,
                Respuesta = d.Respuesta,
                Comentario = d.Comentario,
                Ponderacion = d.Ponderacion
            });
            await svc.UpsertDetallesAsync(id, items, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:int}/finalizar", async (int id, IEvaluacionesService svc, CancellationToken ct) =>
        {
            await svc.FinalizarAsync(id, ct);
            return Results.NoContent();
        });

        return group;
    }
}
