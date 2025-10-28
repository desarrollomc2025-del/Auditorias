using System.Net.Http.Json;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class ApiCategoriasService(HttpClient http) : ICategoriasService
{
    public async Task<IReadOnlyList<Categoria>> GetAllAsync(CancellationToken ct = default)
        => await (http.GetFromJsonAsync<List<Categoria>>("api/categorias", ct) ?? Task.FromResult(new List<Categoria>()));

    public async Task<Categoria?> GetByIdAsync(int categoriaId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<Categoria>($"api/categorias/{categoriaId}", ct);

    public async Task<int> UpsertAsync(Categoria categoria, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("api/categorias", categoria, ct);
        resp.EnsureSuccessStatusCode();
        var id = await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct);
        return id;
    }

    public async Task DeleteAsync(int categoriaId, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"api/categorias/{categoriaId}", ct);
        resp.EnsureSuccessStatusCode();
    }
}

