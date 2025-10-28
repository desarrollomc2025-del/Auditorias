using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Web.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class EvaluadoresEndpoints
{
    public static RouteGroupBuilder MapEvaluadores(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/evaluadores");

        group.MapGet("", async (ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.QueryAsync<Evaluador>(new CommandDefinition(
                "SELECT EvaluadorId, Codigo, Nombre, Activo, FechaCreacion FROM dbo.Evaluadores WHERE Activo = 1",
                cancellationToken: ct));
            return Results.Ok(rows);
        });

        group.MapGet("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var item = await db.QuerySingleOrDefaultAsync<Evaluador>(new CommandDefinition(
                "SELECT EvaluadorId, Codigo, Nombre, Activo, FechaCreacion FROM dbo.Evaluadores WHERE EvaluadorId = @id",
                new { id }, cancellationToken: ct));
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("", async (Evaluador evaluador, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Evaluadores WHERE EvaluadorId=@EvaluadorId)
BEGIN
    UPDATE dbo.Evaluadores SET Codigo=@Codigo, Nombre=@Nombre, Activo=@Activo
    WHERE EvaluadorId=@EvaluadorId;
    SELECT @EvaluadorId;
END
ELSE
BEGIN
    INSERT INTO dbo.Evaluadores(Codigo, Nombre, Activo, FechaCreacion)
    VALUES(@Codigo, @Nombre, @Activo, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
            var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, evaluador, cancellationToken: ct));
            return Results.Ok(id);
        });

        // Soft delete: Activo = 0
        group.MapDelete("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.Evaluadores SET Activo = 0 WHERE EvaluadorId = @id",
                new { id }, cancellationToken: ct));
            return rows == 0 ? Results.NotFound() : Results.NoContent();
        });

        return group;
    }
}

