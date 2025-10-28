using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Web.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class PreguntasEndpoints
{
    public static RouteGroupBuilder MapPreguntas(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/preguntas");

        group.MapGet("", async (ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.QueryAsync<Pregunta>(new CommandDefinition(
                "SELECT PreguntaId, CategoriaId, Codigo, TextoPregunta, Ponderacion, Orden, Activo, FechaCreacion FROM dbo.Preguntas WHERE Activo = 1",
                cancellationToken: ct));
            return Results.Ok(rows);
        });

        group.MapGet("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var item = await db.QuerySingleOrDefaultAsync<Pregunta>(new CommandDefinition(
                "SELECT PreguntaId, CategoriaId, Codigo, TextoPregunta, Ponderacion, Orden, Activo, FechaCreacion FROM dbo.Preguntas WHERE PreguntaId = @id",
                new { id }, cancellationToken: ct));
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("", async (Pregunta pregunta, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Preguntas WHERE PreguntaId=@PreguntaId)
BEGIN
    UPDATE dbo.Preguntas SET CategoriaId=@CategoriaId, Codigo=@Codigo, TextoPregunta=@TextoPregunta, Ponderacion=@Ponderacion, Orden=@Orden, Activo=@Activo
    WHERE PreguntaId=@PreguntaId;
    SELECT @PreguntaId;
END
ELSE
BEGIN
    INSERT INTO dbo.Preguntas(CategoriaId, Codigo, TextoPregunta, Ponderacion, Orden, Activo, FechaCreacion)
    VALUES(@CategoriaId, @Codigo, @TextoPregunta, @Ponderacion, @Orden, @Activo, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
            var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, pregunta, cancellationToken: ct));
            return Results.Ok(id);
        });

        // Soft delete: Activo = 0
        group.MapDelete("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.Preguntas SET Activo = 0 WHERE PreguntaId = @id",
                new { id }, cancellationToken: ct));
            return rows == 0 ? Results.NotFound() : Results.NoContent();
        });

        return group;
    }
}

