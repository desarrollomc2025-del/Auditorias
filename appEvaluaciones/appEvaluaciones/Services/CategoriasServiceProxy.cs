using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class CategoriasServiceProxy : ICategoriasService
{
    private readonly ICategoriasService _online;
    private readonly ICategoriasService _offline;
    private readonly IConnectivity _connectivity;

    public CategoriasServiceProxy(ICategoriasService online, ICategoriasService offline, IConnectivity connectivity)
    {
        _online = online;
        _offline = offline;
        _connectivity = connectivity;
    }

    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    public Task<IReadOnlyList<Categoria>> GetAllAsync(CancellationToken ct = default)
        => IsOnline ? _online.GetAllAsync(ct) : _offline.GetAllAsync(ct);

    public Task<Categoria?> GetByIdAsync(int categoriaId, CancellationToken ct = default)
        => IsOnline ? _online.GetByIdAsync(categoriaId, ct) : _offline.GetByIdAsync(categoriaId, ct);

    public async Task<int> UpsertAsync(Categoria categoria, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            var id = await _online.UpsertAsync(categoria, ct);
            categoria.CategoriaId = id;
            await _offline.UpsertAsync(categoria, ct);
            return id;
        }
        return await _offline.UpsertAsync(categoria, ct);
    }

    public async Task DeleteAsync(int categoriaId, CancellationToken ct = default)
    {
        if (IsOnline)
        {
            await _online.DeleteAsync(categoriaId, ct);
        }
        await _offline.DeleteAsync(categoriaId, ct);
    }
}

