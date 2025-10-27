using appEvaluaciones.Shared.Models;

namespace appEvaluaciones.Shared.Services;

public interface ITiposTiendaService
{
    Task<IReadOnlyList<TipoTienda>> GetAllAsync(CancellationToken ct = default);
}

