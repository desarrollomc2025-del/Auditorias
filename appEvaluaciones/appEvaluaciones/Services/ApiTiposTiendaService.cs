using System.Net.Http.Json;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class ApiTiposTiendaService(HttpClient http) : ITiposTiendaService
{
    public async Task<IReadOnlyList<TipoTienda>> GetAllAsync(CancellationToken ct = default)
        => await (http.GetFromJsonAsync<List<TipoTienda>>("api/tipos-tienda", ct) ?? Task.FromResult(new List<TipoTienda>()));
}

