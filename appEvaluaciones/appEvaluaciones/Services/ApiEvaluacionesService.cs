
using System.Net.Http.Json;
using appEvaluaciones.Shared.Services;
using appEvaluaciones.Shared.Models;

namespace appEvaluaciones.Services;

public sealed class ApiEvaluacionesService(HttpClient http) : IEvaluacionesService
{
    public async Task<int> CreateAsync(int tiendaId, CancellationToken ct = default)
    {
        var payload = new { TiendaId = tiendaId };
        var resp = await http.PostAsJsonAsync("api/evaluaciones", payload, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct))!;
    }

    public async Task UpsertDetalleAsync(int evaluacionId, int preguntaId, bool? respuesta, string? comentario, decimal ponderacion, CancellationToken ct = default)
    {
        var payload = new { PreguntaId = preguntaId, Respuesta = respuesta, Comentario = comentario, Ponderacion = ponderacion };
        var resp = await http.PostAsJsonAsync($"api/evaluaciones/{evaluacionId}/detalle", payload, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task UpsertDetallesAsync(int evaluacionId, IEnumerable<DetalleUpsert> detalles, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"api/evaluaciones/{evaluacionId}/detalles", detalles, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task FinalizarAsync(int evaluacionId, CancellationToken ct = default)
    {
        var resp = await http.PostAsync($"api/evaluaciones/{evaluacionId}/finalizar", content: null, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<EvaluacionVm> GetAsync(int evaluacionId, CancellationToken ct = default)
    {
        var vm = await http.GetFromJsonAsync<EvaluacionVm>($"api/evaluaciones/{evaluacionId}", ct);
        return vm!;
    }
}
