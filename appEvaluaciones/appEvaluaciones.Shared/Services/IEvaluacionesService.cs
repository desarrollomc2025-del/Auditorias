namespace appEvaluaciones.Shared.Services;

public interface IEvaluacionesService
{
    Task<int> CreateAsync(Guid evaluacionKey, int tiendaId, CancellationToken ct = default);
    Task UpsertDetalleAsync(Guid evaluacionKey, int preguntaId, bool? respuesta, string? comentario, decimal ponderacion, CancellationToken ct = default);
    Task UpsertDetallesAsync(Guid evaluacionKey, IEnumerable<DetalleUpsert> detalles, CancellationToken ct = default);
    Task FinalizarAsync(Guid evaluacionKey, CancellationToken ct = default);
    Task<EvaluacionVm> GetAsync(Guid evaluacionKey, CancellationToken ct = default);
}
