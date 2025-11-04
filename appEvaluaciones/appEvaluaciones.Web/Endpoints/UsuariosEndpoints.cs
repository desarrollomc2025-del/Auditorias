using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;

namespace appEvaluaciones.Web.Endpoints;

public static class UsuariosEndpoints
{
    private sealed record UserVm(int UsuarioId, string Usuario, string Rol, int? EvaluadorId, bool Activo, string? Correo, string? Nombre, string? Apellidos);
    private sealed record CreateUserDto(string Usuario, string Password, string Rol, int? EvaluadorId, bool Activo = true, string? Correo = null, string? Nombre = null, string? Apellidos = null);
    private sealed record UpdateUserDto(string? Rol, int? EvaluadorId, bool? Activo, string? Correo, string? Nombre, string? Apellidos);
    private sealed record ChangePasswordDto(string Password);

    public static RouteGroupBuilder MapUsuarios(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usuarios");

        group.MapGet("", async (appEvaluaciones.Web.Services.ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"SELECT UsuarioId, Usuario, Rol, EvaluadorId, COALESCE(Activo,1) AS Activo, Correo, Nombre, Apellidos FROM dbo.Usuarios ORDER BY UsuarioId";
            var rows = await db.QueryAsync<UserVm>(new CommandDefinition(sql, cancellationToken: ct));
            return Results.Ok(rows);
        });

        group.MapGet("/{id:int}", async (int id, appEvaluaciones.Web.Services.ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"SELECT TOP 1 UsuarioId, Usuario, Rol, EvaluadorId, COALESCE(Activo,1) AS Activo, Correo, Nombre, Apellidos FROM dbo.Usuarios WHERE UsuarioId=@id";
            var row = await db.QueryFirstOrDefaultAsync<UserVm>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
            return row is null ? Results.NotFound() : Results.Ok(row);
        });

        group.MapPost("", async (CreateUserDto dto, appEvaluaciones.Web.Services.ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(dto.Password));
            const string sql = @"INSERT INTO dbo.Usuarios(Usuario, Correo, Nombre, Apellidos, Activo, IntentosFallidos, BloqueadoHasta, FechaCreacion, PasswordHash, Rol, EvaluadorId)
VALUES(@Usuario, @Correo, @Nombre, @Apellidos, @Activo, 0, NULL, SYSUTCDATETIME(), @PasswordHash, @Rol, @EvaluadorId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { dto.Usuario, dto.Correo, dto.Nombre, dto.Apellidos, dto.Activo, PasswordHash = hash, dto.Rol, dto.EvaluadorId }, cancellationToken: ct));
            return Results.Ok(id);
        });

        group.MapPut("/{id:int}", async (int id, UpdateUserDto dto, appEvaluaciones.Web.Services.ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"UPDATE dbo.Usuarios
SET Rol = COALESCE(@Rol, Rol),
    EvaluadorId = COALESCE(@EvaluadorId, EvaluadorId),
    Activo = COALESCE(@Activo, Activo),
    Correo = COALESCE(@Correo, Correo),
    Nombre = COALESCE(@Nombre, Nombre),
    Apellidos = COALESCE(@Apellidos, Apellidos)
WHERE UsuarioId = @id;";
            var rows = await db.ExecuteAsync(new CommandDefinition(sql, new { id, dto.Rol, dto.EvaluadorId, dto.Activo, dto.Correo, dto.Nombre, dto.Apellidos }, cancellationToken: ct));
            return rows == 0 ? Results.NotFound() : Results.NoContent();
        });

        group.MapPut("/{id:int}/password", async (int id, ChangePasswordDto dto, appEvaluaciones.Web.Services.ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(dto.Password));
            const string sql = @"UPDATE dbo.Usuarios SET PasswordHash = @hash WHERE UsuarioId = @id";
            var rows = await db.ExecuteAsync(new CommandDefinition(sql, new { id, hash }, cancellationToken: ct));
            return rows == 0 ? Results.NotFound() : Results.NoContent();
        });

        group.MapDelete("/{id:int}", async (int id, appEvaluaciones.Web.Services.ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"DELETE FROM dbo.Usuarios WHERE UsuarioId = @id";
            var rows = await db.ExecuteAsync(new CommandDefinition(sql, new { id }, cancellationToken: ct));
            return rows == 0 ? Results.NotFound() : Results.NoContent();
        });

        return group;
    }
}

