using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class TiposTiendaServiceProxy : ITiposTiendaService
{
    private readonly ITiposTiendaService _online;
    private readonly ITiposTiendaService _offline;
    private readonly IConnectivity _connectivity;

    public TiposTiendaServiceProxy(ITiposTiendaService online, ITiposTiendaService offline, IConnectivity connectivity)
    {
        _online = online;
        _offline = offline;
        _connectivity = connectivity;
    }

    public Task<IReadOnlyList<TipoTienda>> GetAllAsync(CancellationToken ct = default)
        => _connectivity.NetworkAccess == NetworkAccess.Internet ? _online.GetAllAsync(ct) : _offline.GetAllAsync(ct);
}

