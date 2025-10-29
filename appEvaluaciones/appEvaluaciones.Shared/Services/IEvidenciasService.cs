using appEvaluaciones.Shared.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace appEvaluaciones.Shared.Services;

public interface IEvidenciasService
{
    Task<IReadOnlyList<Evidencia>> GetByEvaluacionAsync(Guid evaluacionKey, CancellationToken ct = default);
    Task<Evidencia> AddAsync(Guid evaluacionKey, int preguntaId, string? comentario, string? url = null, CancellationToken ct = default);
    Task<Evidencia> UploadAsync(Guid evaluacionKey, int preguntaId, string? comentario, IBrowserFile file, CancellationToken ct = default);
}

