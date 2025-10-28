using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Web.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class GerentesEndpoints
{
    public static RouteGroupBuilder MapGerentes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gerentes");

        group.MapGet("", async (ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.QueryAsync<Gerente>(new CommandDefinition(
                "SELECT GerenteId, Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion FROM dbo.Gerentes WHERE Activo = 1",
                cancellationToken: ct));
            return Results.Ok(rows);
        });

        group.MapGet("/regionales", async (ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.QueryAsync<Gerente>(new CommandDefinition(
                "SELECT GerenteId, Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion FROM dbo.Gerentes WHERE Activo = 1 AND Rol = 'Regional'",
                cancellationToken: ct));
            return Results.Ok(rows);
        });

        group.MapGet("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var item = await db.QuerySingleOrDefaultAsync<Gerente>(new CommandDefinition(
                "SELECT GerenteId, Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion FROM dbo.Gerentes WHERE GerenteId = @id",
                new { id }, cancellationToken: ct));
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("", async (Gerente gerente, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            // Validación simple para regla de jerarquía
            if (string.Equals(gerente.Rol, "Regional", StringComparison.OrdinalIgnoreCase))
            {
                gerente.GerenteRegionalId = null;
            }
            else if (string.Equals(gerente.Rol, "Area", StringComparison.OrdinalIgnoreCase))
            {
                if (gerente.GerenteRegionalId is null)
                    return Results.BadRequest("GerenteRegionalId es requerido para Rol 'Area'.");
            }

            using IDbConnection db = factory.Create();
            const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Gerentes WHERE GerenteId=@GerenteId)
BEGIN
    UPDATE dbo.Gerentes SET Codigo=@Codigo, Nombre=@Nombre, Rol=@Rol, GerenteRegionalId=@GerenteRegionalId, Activo=@Activo
    WHERE GerenteId=@GerenteId;
    SELECT @GerenteId;
END
ELSE
BEGIN
    INSERT INTO dbo.Gerentes(Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion)
    VALUES(@Codigo, @Nombre, @Rol, @GerenteRegionalId, @Activo, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
            var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, gerente, cancellationToken: ct));
            return Results.Ok(id);
        });

        // Soft delete: Activo = 0
        group.MapDelete("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.Gerentes SET Activo = 0 WHERE GerenteId = @id",
                new { id }, cancellationToken: ct));
            return rows == 0 ? Results.NotFound() : Results.NoContent();
        });

        return group;
    }
}

