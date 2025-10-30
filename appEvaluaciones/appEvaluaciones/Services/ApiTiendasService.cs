using System.Net.Http.Json;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class ApiTiendasService(HttpClient http) : ITiendasService
{
    public async Task<IReadOnlyList<Tienda>> GetAllAsync(CancellationToken ct = default)
        => (await http.GetFromJsonAsync<List<Tienda>>("api/tiendas", ct)) ?? new List<Tienda>();

    public async Task<Tienda?> GetByIdAsync(int tiendaId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<Tienda>($"api/tiendas/{tiendaId}", ct);

    public async Task<int> UpsertAsync(Tienda tienda, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("api/tiendas", tienda, ct);
        resp.EnsureSuccessStatusCode();
        var id = await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct);
        return id;
    }

    public async Task DeleteAsync(int tiendaId, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"api/tiendas/{tiendaId}", ct);
        resp.EnsureSuccessStatusCode();
    }
}
