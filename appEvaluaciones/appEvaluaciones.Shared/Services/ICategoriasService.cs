using appEvaluaciones.Shared.Models;

namespace appEvaluaciones.Shared.Services;

public interface ICategoriasService
{
    Task<IReadOnlyList<Categoria>> GetAllAsync(CancellationToken ct = default);
    Task<Categoria?> GetByIdAsync(int categoriaId, CancellationToken ct = default);
    Task<int> UpsertAsync(Categoria categoria, CancellationToken ct = default);
    Task DeleteAsync(int categoriaId, CancellationToken ct = default);
}

