using System.Data;
using Dapper;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class EvaluacionesEndpoints
{
    private sealed record CreateEvaluacionDto(Guid EvaluacionKey, int TiendaId);
    private sealed record DetalleDto(int PreguntaId, bool? Respuesta, string? Comentario, decimal Ponderacion);

    public static RouteGroupBuilder MapEvaluaciones(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/evaluaciones");

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
