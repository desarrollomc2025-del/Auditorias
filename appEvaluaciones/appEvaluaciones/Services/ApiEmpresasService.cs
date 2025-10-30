using System.Net.Http.Json;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class ApiEmpresasService(HttpClient http) : IEmpresasService
{
    public async Task<IReadOnlyList<Empresa>> GetAllAsync(CancellationToken ct = default)
        => (await http.GetFromJsonAsync<List<Empresa>>("api/empresas", ct)) ?? new List<Empresa>();

    public async Task<Empresa?> GetByIdAsync(int empresaId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<Empresa>($"api/empresas/{empresaId}", ct);

    public async Task<int> UpsertAsync(Empresa empresa, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("api/empresas", empresa, ct);
        resp.EnsureSuccessStatusCode();
        var id = await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct);
        return id;
    }

    public async Task DeleteAsync(int empresaId, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"api/empresas/{empresaId}", ct);
        resp.EnsureSuccessStatusCode();
    }
}

