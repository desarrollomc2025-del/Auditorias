using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Web.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class CategoriasEndpoints
{
    public static RouteGroupBuilder MapCategorias(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categorias");

        group.MapGet("", async (ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.QueryAsync<Categoria>(new CommandDefinition(
                "SELECT CategoriaId, Codigo, Descripcion, Ponderacion, Activo, FechaCreacion FROM dbo.Categorias WHERE Activo = 1",
                cancellationToken: ct));
            return Results.Ok(rows);
        });

        group.MapGet("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var item = await db.QuerySingleOrDefaultAsync<Categoria>(new CommandDefinition(
                "SELECT CategoriaId, Codigo, Descripcion, Ponderacion, Activo, FechaCreacion FROM dbo.Categorias WHERE CategoriaId = @id",
                new { id }, cancellationToken: ct));
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("", async (Categoria categoria, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Categorias WHERE CategoriaId=@CategoriaId)
BEGIN
    UPDATE dbo.Categorias SET Codigo=@Codigo, Descripcion=@Descripcion, Ponderacion=@Ponderacion, Activo=@Activo
    WHERE CategoriaId=@CategoriaId;
    SELECT @CategoriaId;
END
ELSE
BEGIN
    INSERT INTO dbo.Categorias(Codigo, Descripcion, Ponderacion, Activo, FechaCreacion)
    VALUES(@Codigo, @Descripcion, @Ponderacion, @Activo, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
            var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, categoria, cancellationToken: ct));
            return Results.Ok(id);
        });

        // Soft delete: Activo = 0
        group.MapDelete("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.Categorias SET Activo = 0 WHERE CategoriaId = @id",
                new { id }, cancellationToken: ct));
            return rows == 0 ? Results.NotFound() : Results.NoContent();
        });

        return group;
    }
}

