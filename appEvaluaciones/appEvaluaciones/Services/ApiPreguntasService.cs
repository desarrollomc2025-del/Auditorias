using System.Net.Http.Json;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class ApiPreguntasService(HttpClient http) : IPreguntasService
{
    public async Task<IReadOnlyList<Pregunta>> GetAllAsync(CancellationToken ct = default)
        => (await http.GetFromJsonAsync<List<Pregunta>>("api/preguntas", ct)) ?? new List<Pregunta>();

    public async Task<Pregunta?> GetByIdAsync(int preguntaId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<Pregunta>($"api/preguntas/{preguntaId}", ct);

    public async Task<int> UpsertAsync(Pregunta pregunta, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("api/preguntas", pregunta, ct);
        resp.EnsureSuccessStatusCode();
        var id = await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct);
        return id;
    }

    public async Task DeleteAsync(int preguntaId, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"api/preguntas/{preguntaId}", ct);
        resp.EnsureSuccessStatusCode();
    }
}

