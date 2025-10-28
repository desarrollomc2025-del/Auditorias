using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class GerentesServiceProxy : IGerentesService
{
    private readonly IGerentesService _online;
    private readonly IGerentesService _offline;
    private readonly IConnectivity _connectivity;

    public GerentesServiceProxy(IGerentesService online, IGerentesService offline, IConnectivity connectivity)
    {
        _online = online;
        _offline = offline;
        _connectivity = connectivity;
    }

    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    public Task<IReadOnlyList<Gerente>> GetAllAsync(CancellationToken ct = default)
        => IsOnline ? _online.GetAllAsync(ct) : _offline.GetAllAsync(ct);

    public Task<Gerente?> GetByIdAsync(int gerenteId, CancellationToken ct = default)
        => IsOnline ? _online.GetByIdAsync(gerenteId, ct) : _offline.GetByIdAsync(gerenteId, ct);

    public Task<IReadOnlyList<Gerente>> GetRegionalesAsync(CancellationToken ct = default)
        => IsOnline ? _online.GetRegionalesAsync(ct) : _offline.GetRegionalesAsync(ct);

    public async Task<int> UpsertAsync(Gerente gerente, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            var id = await _online.UpsertAsync(gerente, ct);
            gerente.GerenteId = id;
            await _offline.UpsertAsync(gerente, ct);
            return id;
        }
        return await _offline.UpsertAsync(gerente, ct);
    }

    public async Task DeleteAsync(int gerenteId, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            await _online.DeleteAsync(gerenteId, ct);
        }
        await _offline.DeleteAsync(gerenteId, ct);
    }
}

