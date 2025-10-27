using appEvaluaciones.Shared.Models;

namespace appEvaluaciones.Shared.Services;

public interface IEmpresasService
{
    Task<IReadOnlyList<Empresa>> GetAllAsync(CancellationToken ct = default);
    Task<Empresa?> GetByIdAsync(int empresaId, CancellationToken ct = default);
    Task<int> UpsertAsync(Empresa empresa, CancellationToken ct = default);
    Task DeleteAsync(int empresaId, CancellationToken ct = default);
}

