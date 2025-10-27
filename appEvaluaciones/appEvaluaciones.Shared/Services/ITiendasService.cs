using appEvaluaciones.Shared.Models;

namespace appEvaluaciones.Shared.Services;

public interface ITiendasService
{
    Task<IReadOnlyList<Tienda>> GetAllAsync(CancellationToken ct = default);
    Task<Tienda?> GetByIdAsync(int tiendaId, CancellationToken ct = default);
    Task<int> UpsertAsync(Tienda tienda, CancellationToken ct = default);
    Task DeleteAsync(int tiendaId, CancellationToken ct = default);
}
