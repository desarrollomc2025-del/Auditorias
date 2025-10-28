using appEvaluaciones.Shared.Models;

namespace appEvaluaciones.Shared.Services;

public interface IGerentesService
{
    Task<IReadOnlyList<Gerente>> GetAllAsync(CancellationToken ct = default);
    Task<Gerente?> GetByIdAsync(int gerenteId, CancellationToken ct = default);
    Task<IReadOnlyList<Gerente>> GetRegionalesAsync(CancellationToken ct = default);
    Task<int> UpsertAsync(Gerente gerente, CancellationToken ct = default);
    Task DeleteAsync(int gerenteId, CancellationToken ct = default);
}

