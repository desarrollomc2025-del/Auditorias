using System.Net.Http.Json;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class ApiEvaluadoresService(HttpClient http) : IEvaluadoresService
{
    public async Task<IReadOnlyList<Evaluador>> GetAllAsync(CancellationToken ct = default)
        => (await http.GetFromJsonAsync<List<Evaluador>>("api/evaluadores", ct)) ?? new List<Evaluador>();

    public async Task<Evaluador?> GetByIdAsync(int evaluadorId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<Evaluador>($"api/evaluadores/{evaluadorId}", ct);

    public async Task<int> UpsertAsync(Evaluador evaluador, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("api/evaluadores", evaluador, ct);
        resp.EnsureSuccessStatusCode();
        var id = await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct);
        return id;
    }

    public async Task DeleteAsync(int evaluadorId, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"api/evaluadores/{evaluadorId}", ct);
        resp.EnsureSuccessStatusCode();
    }
}

