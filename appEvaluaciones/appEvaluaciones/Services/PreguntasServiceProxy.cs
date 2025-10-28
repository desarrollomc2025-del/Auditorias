using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class PreguntasServiceProxy : IPreguntasService
{
    private readonly IPreguntasService _online;
    private readonly IPreguntasService _offline;
    private readonly IConnectivity _connectivity;

    public PreguntasServiceProxy(IPreguntasService online, IPreguntasService offline, IConnectivity connectivity)
    {
        _online = online;
        _offline = offline;
        _connectivity = connectivity;
    }

    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    public Task<IReadOnlyList<Pregunta>> GetAllAsync(CancellationToken ct = default)
        => IsOnline ? _online.GetAllAsync(ct) : _offline.GetAllAsync(ct);

    public Task<Pregunta?> GetByIdAsync(int preguntaId, CancellationToken ct = default)
        => IsOnline ? _online.GetByIdAsync(preguntaId, ct) : _offline.GetByIdAsync(preguntaId, ct);

    public async Task<int> UpsertAsync(Pregunta pregunta, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            var id = await _online.UpsertAsync(pregunta, ct);
            pregunta.PreguntaId = id;
            await _offline.UpsertAsync(pregunta, ct);
            return id;
        }
        return await _offline.UpsertAsync(pregunta, ct);
    }

    public async Task DeleteAsync(int preguntaId, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            await _online.DeleteAsync(preguntaId, ct);
        }
        await _offline.DeleteAsync(preguntaId, ct);
    }
}

