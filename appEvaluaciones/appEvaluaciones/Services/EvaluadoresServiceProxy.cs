using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class EvaluadoresServiceProxy : IEvaluadoresService
{
    private readonly IEvaluadoresService _online;
    private readonly IEvaluadoresService _offline;
    private readonly IConnectivity _connectivity;

    public EvaluadoresServiceProxy(IEvaluadoresService online, IEvaluadoresService offline, IConnectivity connectivity)
    {
        _online = online;
        _offline = offline;
        _connectivity = connectivity;
    }

    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    public Task<IReadOnlyList<Evaluador>> GetAllAsync(CancellationToken ct = default)
        => IsOnline ? _online.GetAllAsync(ct) : _offline.GetAllAsync(ct);

    public Task<Evaluador?> GetByIdAsync(int evaluadorId, CancellationToken ct = default)
        => IsOnline ? _online.GetByIdAsync(evaluadorId, ct) : _offline.GetByIdAsync(evaluadorId, ct);

    public async Task<int> UpsertAsync(Evaluador evaluador, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            var id = await _online.UpsertAsync(evaluador, ct);
            evaluador.EvaluadorId = id;
            await _offline.UpsertAsync(evaluador, ct);
            return id;
        }
        return await _offline.UpsertAsync(evaluador, ct);
    }

    public async Task DeleteAsync(int evaluadorId, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            await _online.DeleteAsync(evaluadorId, ct);
        }
        await _offline.DeleteAsync(evaluadorId, ct);
    }
}

