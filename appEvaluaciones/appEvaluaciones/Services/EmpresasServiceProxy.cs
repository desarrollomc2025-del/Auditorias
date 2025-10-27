using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class EmpresasServiceProxy : IEmpresasService
{
    private readonly IEmpresasService _online;
    private readonly IEmpresasService _offline;
    private readonly IConnectivity _connectivity;

    public EmpresasServiceProxy(IEmpresasService online, IEmpresasService offline, IConnectivity connectivity)
    {
        _online = online;
        _offline = offline;
        _connectivity = connectivity;
    }

    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    public Task<IReadOnlyList<Empresa>> GetAllAsync(CancellationToken ct = default)
        => IsOnline ? _online.GetAllAsync(ct) : _offline.GetAllAsync(ct);

    public Task<Empresa?> GetByIdAsync(int empresaId, CancellationToken ct = default)
        => IsOnline ? _online.GetByIdAsync(empresaId, ct) : _offline.GetByIdAsync(empresaId, ct);

    public async Task<int> UpsertAsync(Empresa empresa, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            var id = await _online.UpsertAsync(empresa, ct);
            empresa.EmpresaId = id;
            await _offline.UpsertAsync(empresa, ct);
            return id;
        }
        return await _offline.UpsertAsync(empresa, ct);
    }

    public async Task DeleteAsync(int empresaId, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            await _online.DeleteAsync(empresaId, ct);
        }
        await _offline.DeleteAsync(empresaId, ct);
    }
}

