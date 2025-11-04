using appEvaluaciones.Shared.Models;

namespace appEvaluaciones.Shared.Services;

public interface IEvaluacionesService
{
    Task<int> CreateAsync(int tiendaId, int? evaluadorId = null, CancellationToken ct = default);
    Task UpsertDetalleAsync(int evaluacionId, int preguntaId, bool? respuesta, string? comentario, decimal ponderacion, CancellationToken ct = default);
    Task UpsertDetallesAsync(int evaluacionId, IEnumerable<DetalleUpsert> detalles, CancellationToken ct = default);
    Task FinalizarAsync(int evaluacionId, CancellationToken ct = default);
    Task<EvaluacionVm> GetAsync(int evaluacionId, CancellationToken ct = default);
}
