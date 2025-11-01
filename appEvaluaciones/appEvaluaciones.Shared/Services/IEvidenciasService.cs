using appEvaluaciones.Shared.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace appEvaluaciones.Shared.Services;

public interface IEvidenciasService
{
    Task<IReadOnlyList<Evidencia>> GetByEvaluacionAsync(int evaluacionId, CancellationToken ct = default);
    Task<Evidencia> AddAsync(int evaluacionId, int preguntaId, string? comentario, string? url = null, CancellationToken ct = default);
    Task<Evidencia> UploadAsync(int evaluacionId, int preguntaId, string? comentario, IBrowserFile file, CancellationToken ct = default);
}

