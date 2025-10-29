using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace appEvaluaciones.Services;

public sealed class ApiEvidenciasService(HttpClient http) : IEvidenciasService
{
    public async Task<IReadOnlyList<Evidencia>> GetByEvaluacionAsync(Guid evaluacionKey, CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<Evidencia>>($"api/evidencias/{evaluacionKey}", ct) ?? new List<Evidencia>();

    public async Task<Evidencia> AddAsync(Guid evaluacionKey, int preguntaId, string? comentario, string? url = null, CancellationToken ct = default)
    {
        var payload = new Evidencia { EvaluacionKey = evaluacionKey, PreguntaId = preguntaId, Comentario = comentario, Url = url };
        var resp = await http.PostAsJsonAsync("api/evidencias", payload, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<Evidencia>(cancellationToken: ct))!;
    }

    public async Task<Evidencia> UploadAsync(Guid evaluacionKey, int preguntaId, string? comentario, IBrowserFile file, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(evaluacionKey.ToString()), "evaluacionKey");
        content.Add(new StringContent(preguntaId.ToString()), "preguntaId");
        content.Add(new StringContent(comentario ?? string.Empty), "comentario");

        var stream = file.OpenReadStream(5 * 1024 * 1024); // 5 MB
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.Name);

        var resp = await http.PostAsync("api/evidencias/upload", content, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<Evidencia>(cancellationToken: ct))!;
    }
}
