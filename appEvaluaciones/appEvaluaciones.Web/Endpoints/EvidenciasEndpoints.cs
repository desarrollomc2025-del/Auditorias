using System.Text.RegularExpressions;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class EvidenciasEndpoints
{
    public static RouteGroupBuilder MapEvidencias(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/evidencias");

        group.MapGet("/{evaluacionId:int}", async (int evaluacionId, IEvidenciasService svc, CancellationToken ct)
            => Results.Ok(await svc.GetByEvaluacionAsync(evaluacionId, ct)));

        // JSON add (comentario y/o url)
        group.MapPost("", async (Evidencia ev, IEvidenciasService svc, CancellationToken ct)
            => Results.Ok(await svc.AddAsync(ev.EvaluacionId, ev.PreguntaId, ev.Comentario, ev.Url, ct)));

        // File upload (multipart/form-data)
        group.MapPost("/upload", async (HttpRequest request, IWebHostEnvironment env, IEvidenciasService svc, CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Content-Type debe ser multipart/form-data");

            var form = await request.ReadFormAsync(ct);
            if (!int.TryParse(form["evaluacionId"], out var evaluacionId))
                return Results.BadRequest("evaluacionId inválido");

            if (!int.TryParse(form["preguntaId"], out var preguntaId))
                return Results.BadRequest("preguntaId inválido");

            var comentario = form["comentario"].ToString();
            var file = form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest("Archivo requerido");

            // Sanitize filename
            var fileName = Regex.Replace(Path.GetFileName(file.FileName), "[^A-Za-z0-9_.-]", "_");
            var relFolder = Path.Combine("evidencias", evaluacionId.ToString());
            var absFolder = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), relFolder);
            Directory.CreateDirectory(absFolder);

            var absPath = Path.Combine(absFolder, fileName);
            await using (var fs = new FileStream(absPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(fs, ct);
            }

            var url = "/" + Path.Combine(relFolder, fileName).Replace("\\", "/");

            // Delegate DB insertion to service (uses correct schema)
            var ev = await svc.AddAsync(evaluacionId, preguntaId, string.IsNullOrWhiteSpace(comentario) ? null : comentario, url, ct);
            return Results.Ok(ev);
        });

        return group;
    }
}

