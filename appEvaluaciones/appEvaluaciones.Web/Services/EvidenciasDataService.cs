using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace appEvaluaciones.Web.Services;

public sealed class EvidenciasDataService(ISqlConnectionFactory factory, IWebHostEnvironment env) : IEvidenciasService
{
    public async Task<IReadOnlyList<Evidencia>> GetByEvaluacionAsync(Guid evaluacionKey, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        var rows = await db.QueryAsync<Evidencia>(new CommandDefinition(
            "SELECT EvidenciaId, EvaluacionKey, PreguntaId, Comentario, Url, FechaCreacion FROM dbo.Evidencias WHERE EvaluacionKey = @evaluacionKey",
            new { evaluacionKey }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Evidencia> AddAsync(Guid evaluacionKey, int preguntaId, string? comentario, string? url = null, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"INSERT INTO dbo.Evidencias(EvaluacionKey, PreguntaId, Comentario, Url, FechaCreacion)
VALUES(@EvaluacionKey, @PreguntaId, @Comentario, @Url, SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
        var ev = new Evidencia
        {
            EvaluacionKey = evaluacionKey,
            PreguntaId = preguntaId,
            Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario,
            Url = url
        };
        var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, ev, cancellationToken: ct));
        ev.EvidenciaId = id;
        ev.FechaCreacion = DateTime.UtcNow;
        return ev;
    }

    public async Task<Evidencia> UploadAsync(Guid evaluacionKey, int preguntaId, string? comentario, IBrowserFile file, CancellationToken ct = default)
    {
        var safeName = Regex.Replace(Path.GetFileName(file.Name), "[^A-Za-z0-9_.-]", "_");
        var relFolder = Path.Combine("evidencias", evaluacionKey.ToString());
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var absFolder = Path.Combine(webRoot, relFolder);
        Directory.CreateDirectory(absFolder);

        var absPath = Path.Combine(absFolder, safeName);
        // Evitar colisiones simples
        if (File.Exists(absPath))
        {
            var name = Path.GetFileNameWithoutExtension(safeName);
            var ext = Path.GetExtension(safeName);
            safeName = $"{name}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
            absPath = Path.Combine(absFolder, safeName);
        }

        await using (var fs = new FileStream(absPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.OpenReadStream(5 * 1024 * 1024).CopyToAsync(fs, ct);
        }

        var url = "/" + Path.Combine(relFolder, safeName).Replace("\\", "/");
        return await AddAsync(evaluacionKey, preguntaId, comentario, url, ct);
    }
}

