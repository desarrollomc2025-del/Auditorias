using appEvaluaciones.Shared.Models;

namespace appEvaluaciones.Shared.Services;

public interface IPreguntasService
{
    Task<IReadOnlyList<Pregunta>> GetAllAsync(CancellationToken ct = default);
    Task<Pregunta?> GetByIdAsync(int preguntaId, CancellationToken ct = default);
    Task<int> UpsertAsync(Pregunta pregunta, CancellationToken ct = default);
    Task DeleteAsync(int preguntaId, CancellationToken ct = default);
}

