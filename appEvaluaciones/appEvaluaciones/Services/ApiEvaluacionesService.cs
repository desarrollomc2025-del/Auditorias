
using System.Net.Http.Json;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class ApiEvaluacionesService(HttpClient http) : IEvaluacionesService
{
    public async Task<int> CreateAsync(Guid evaluacionKey, int tiendaId, CancellationToken ct = default)
    {
        var payload = new { EvaluacionKey = evaluacionKey, TiendaId = tiendaId };
        var resp = await http.PostAsJsonAsync("api/evaluaciones", payload, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct))!;
    }

    public async Task UpsertDetalleAsync(Guid evaluacionKey, int preguntaId, bool? respuesta, string? comentario, decimal ponderacion, CancellationToken ct = default)
    {
        var payload = new { PreguntaId = preguntaId, Respuesta = respuesta, Comentario = comentario, Ponderacion = ponderacion };
        var resp = await http.PostAsJsonAsync($"api/evaluaciones/{evaluacionKey}/detalle", payload, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task UpsertDetallesAsync(Guid evaluacionKey, IEnumerable<DetalleUpsert> detalles, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"api/evaluaciones/{evaluacionKey}/detalles", detalles, ct);
        resp.EnsureSuccessStatusCode();
    }
}
