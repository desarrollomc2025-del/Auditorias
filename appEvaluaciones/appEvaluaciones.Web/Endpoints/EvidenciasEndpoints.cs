using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;
using appEvaluaciones.Web.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class EvidenciasEndpoints
{
    public static RouteGroupBuilder MapEvidencias(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/evidencias");

        group.MapGet("/{evaluacionKey:guid}", async (Guid evaluacionKey, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.QueryAsync<Evidencia>(new CommandDefinition(
                "SELECT EvidenciaId, EvaluacionKey, PreguntaId, Comentario, Url, FechaCreacion FROM dbo.Evidencias WHERE EvaluacionKey = @evaluacionKey",
                new { evaluacionKey }, cancellationToken: ct));
            return Results.Ok(rows);
        });

        // JSON add (comentario y/o url)
        group.MapPost("", async (Evidencia ev, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"INSERT INTO dbo.Evidencias(EvaluacionKey, PreguntaId, Comentario, Url, FechaCreacion)
VALUES(@EvaluacionKey, @PreguntaId, @Comentario, @Url, SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, ev, cancellationToken: ct));
            ev.EvidenciaId = id;
            ev.FechaCreacion = DateTime.UtcNow;
            return Results.Ok(ev);
        });

        // File upload (multipart/form-data)
        group.MapPost("/upload", async (HttpRequest request, IWebHostEnvironment env, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Content-Type debe ser multipart/form-data");

            var form = await request.ReadFormAsync(ct);
            if (!Guid.TryParse(form["evaluacionKey"], out var evaluacionKey))
                return Results.BadRequest("evaluacionKey inválido");

            if (!int.TryParse(form["preguntaId"], out var preguntaId))
                return Results.BadRequest("preguntaId inválido");

            var comentario = form["comentario"].ToString();
            var file = form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest("Archivo requerido");

            // Sanitize filename
            var fileName = Regex.Replace(Path.GetFileName(file.FileName), "[^A-Za-z0-9_.-]", "_");
            var relFolder = Path.Combine("evidencias", evaluacionKey.ToString());
            var absFolder = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), relFolder);
            Directory.CreateDirectory(absFolder);

            var absPath = Path.Combine(absFolder, fileName);
            await using (var fs = new FileStream(absPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(fs, ct);
            }

            var url = "/" + Path.Combine(relFolder, fileName).Replace("\\", "/");

            // Insert DB
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

            return Results.Ok(ev);
        });

        return group;
    }
}

