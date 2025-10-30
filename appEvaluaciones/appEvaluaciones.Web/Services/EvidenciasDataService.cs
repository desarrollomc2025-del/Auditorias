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
        const string sql = @"SELECT evi.EvidenciaId,
       ev.EvaluacionKey,
       d.PreguntaId,
       evi.Descripcion   AS Comentario,
       evi.UrlArchivo    AS Url,
       evi.FechaCreacion
FROM dbo.Evidencias evi
JOIN dbo.DetalleEvaluaciones d ON d.DetalleId = evi.DetalleId
JOIN dbo.Evaluaciones ev ON ev.EvaluacionId = d.EvaluacionId
WHERE ev.EvaluacionKey = @evaluacionKey
ORDER BY evi.EvidenciaId;";
        var rows = await db.QueryAsync<Evidencia>(new CommandDefinition(sql, new { evaluacionKey }, cancellationToken: ct, commandTimeout: 60));
        return rows.ToList();
    }

    public async Task<Evidencia> AddAsync(Guid evaluacionKey, int preguntaId, string? comentario, string? url = null, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"DECLARE @EvalId INT = (SELECT EvaluacionId FROM dbo.Evaluaciones WHERE EvaluacionKey=@evaluacionKey);
IF @EvalId IS NULL THROW 50000, 'Evaluacion no existe', 1;

DECLARE @DetalleId INT = (SELECT DetalleId FROM dbo.DetalleEvaluaciones WHERE EvaluacionId=@EvalId AND PreguntaId=@preguntaId);
IF @DetalleId IS NULL
BEGIN
    INSERT INTO dbo.DetalleEvaluaciones(EvaluacionId, PreguntaId, Respuesta, Comentario, Ponderacion)
    VALUES(@EvalId, @preguntaId, NULL, @comentario, 0);
    SET @DetalleId = CAST(SCOPE_IDENTITY() AS INT);
END

INSERT INTO dbo.Evidencias(DetalleId, Descripcion, UrlArchivo, NombreArchivo, FechaCreacion)
VALUES(@DetalleId, @comentario, @url, NULL, SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { evaluacionKey, preguntaId, comentario, url }, cancellationToken: ct, commandTimeout: 60));

        return new Evidencia
        {
            EvidenciaId = id,
            EvaluacionKey = evaluacionKey,
            PreguntaId = preguntaId,
            Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario,
            Url = url,
            FechaCreacion = DateTime.UtcNow
        };
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

        using IDbConnection db = factory.Create();
        const string sql = @"DECLARE @EvalId INT = (SELECT EvaluacionId FROM dbo.Evaluaciones WHERE EvaluacionKey=@evaluacionKey);
IF @EvalId IS NULL THROW 50000, 'Evaluacion no existe', 1;

DECLARE @DetalleId INT = (SELECT DetalleId FROM dbo.DetalleEvaluaciones WHERE EvaluacionId=@EvalId AND PreguntaId=@preguntaId);
IF @DetalleId IS NULL
BEGIN
    INSERT INTO dbo.DetalleEvaluaciones(EvaluacionId, PreguntaId, Respuesta, Comentario, Ponderacion)
    VALUES(@EvalId, @preguntaId, NULL, @comentario, 0);
    SET @DetalleId = CAST(SCOPE_IDENTITY() AS INT);
END

INSERT INTO dbo.Evidencias(DetalleId, Descripcion, UrlArchivo, NombreArchivo, FechaCreacion)
VALUES(@DetalleId, @comentario, @url, @fileName, SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var newId = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql,
            new { evaluacionKey, preguntaId, comentario, url, fileName = safeName }, cancellationToken: ct, commandTimeout: 60));

        return new Evidencia
        {
            EvidenciaId = newId,
            EvaluacionKey = evaluacionKey,
            PreguntaId = preguntaId,
            Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario,
            Url = url,
            FechaCreacion = DateTime.UtcNow
        };
    }
}

