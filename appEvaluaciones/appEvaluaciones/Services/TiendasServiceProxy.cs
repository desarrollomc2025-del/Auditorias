using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class TiendasServiceProxy : ITiendasService
{
    private readonly ITiendasService _online;
    private readonly ITiendasService _offline;
    private readonly IConnectivity _connectivity;

    public TiendasServiceProxy(ITiendasService online, ITiendasService offline, IConnectivity connectivity)
    {
        _online = online;
        _offline = offline;
        _connectivity = connectivity;
    }

    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    public Task<IReadOnlyList<Tienda>> GetAllAsync(CancellationToken ct = default)
        => IsOnline ? _online.GetAllAsync(ct) : _offline.GetAllAsync(ct);

    public Task<Tienda?> GetByIdAsync(int tiendaId, CancellationToken ct = default)
        => IsOnline ? _online.GetByIdAsync(tiendaId, ct) : _offline.GetByIdAsync(tiendaId, ct);

    public async Task<int> UpsertAsync(Tienda tienda, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            var id = await _online.UpsertAsync(tienda, ct);
            // Espejar en cache local
            tienda.TiendaId = id;
            await _offline.UpsertAsync(tienda, ct);
            return id;
        }
        // Offline: guarda en cache; (opcional) marcar como pendiente de sincronización
        return await _offline.UpsertAsync(tienda, ct);
    }

    public async Task DeleteAsync(int tiendaId, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            await _online.DeleteAsync(tiendaId, ct);
        }
        await _offline.DeleteAsync(tiendaId, ct);
    }
}
