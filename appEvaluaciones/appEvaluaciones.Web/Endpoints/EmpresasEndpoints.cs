using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Web.Services;

namespace appEvaluaciones.Web.Endpoints;

public static class EmpresasEndpoints
{
    public static RouteGroupBuilder MapEmpresas(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/empresas");

        group.MapGet("", async (ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.QueryAsync<Empresa>(new CommandDefinition(
                "SELECT EmpresaId, Codigo, Nombre, Direccion, Eliminado, FechaCreacion FROM dbo.Empresas WHERE Eliminado = 0",
                cancellationToken: ct));
            return Results.Ok(rows);
        });

        group.MapGet("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var e = await db.QuerySingleOrDefaultAsync<Empresa>(new CommandDefinition(
                "SELECT EmpresaId, Codigo, Nombre, Direccion, Eliminado, FechaCreacion FROM dbo.Empresas WHERE EmpresaId = @id",
                new { id }, cancellationToken: ct));
            return e is null ? Results.NotFound() : Results.Ok(e);
        });

        group.MapPost("", async (Empresa empresa, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Empresas WHERE EmpresaId=@EmpresaId)
BEGIN
    UPDATE dbo.Empresas SET Codigo=@Codigo, Nombre=@Nombre, Direccion=@Direccion, Eliminado=@Eliminado
    WHERE EmpresaId=@EmpresaId;
    SELECT @EmpresaId;
END
ELSE
BEGIN
    INSERT INTO dbo.Empresas(Codigo, Nombre, Direccion, Eliminado, FechaCreacion)
    VALUES(@Codigo, @Nombre, @Direccion, @Eliminado, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
            var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, empresa, cancellationToken: ct));
            return Results.Ok(id);
        });

        group.MapDelete("/{id:int}", async (int id, ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var rows = await db.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.Empresas SET Eliminado = 1 WHERE EmpresaId = @id",
                new { id }, cancellationToken: ct));
            return rows == 0 ? Results.NotFound() : Results.NoContent();
        });

        return group;
    }
}

