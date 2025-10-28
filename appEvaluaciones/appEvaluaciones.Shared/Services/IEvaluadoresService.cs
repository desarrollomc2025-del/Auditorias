using appEvaluaciones.Shared.Models;

namespace appEvaluaciones.Shared.Services;

public interface IEvaluadoresService
{
    Task<IReadOnlyList<Evaluador>> GetAllAsync(CancellationToken ct = default);
    Task<Evaluador?> GetByIdAsync(int evaluadorId, CancellationToken ct = default);
    Task<int> UpsertAsync(Evaluador evaluador, CancellationToken ct = default);
    Task DeleteAsync(int evaluadorId, CancellationToken ct = default);
}

