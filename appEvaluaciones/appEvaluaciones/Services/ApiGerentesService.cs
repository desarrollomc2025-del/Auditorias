using System.Net.Http.Json;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class ApiGerentesService(HttpClient http) : IGerentesService
{
    public async Task<IReadOnlyList<Gerente>> GetAllAsync(CancellationToken ct = default)
        => (await http.GetFromJsonAsync<List<Gerente>>("api/gerentes", ct)) ?? new List<Gerente>();

    public async Task<Gerente?> GetByIdAsync(int gerenteId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<Gerente>($"api/gerentes/{gerenteId}", ct);

    public async Task<IReadOnlyList<Gerente>> GetRegionalesAsync(CancellationToken ct = default)
        => (await http.GetFromJsonAsync<List<Gerente>>("api/gerentes/regionales", ct)) ?? new List<Gerente>();

    public async Task<int> UpsertAsync(Gerente gerente, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("api/gerentes", gerente, ct);
        resp.EnsureSuccessStatusCode();
        var id = await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct);
        return id;
    }

    public async Task DeleteAsync(int gerenteId, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"api/gerentes/{gerenteId}", ct);
        resp.EnsureSuccessStatusCode();
    }
}

