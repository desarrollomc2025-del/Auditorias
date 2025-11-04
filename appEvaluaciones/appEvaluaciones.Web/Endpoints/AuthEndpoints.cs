using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using appEvaluaciones.Web.Auth;

namespace appEvaluaciones.Web.Endpoints;

public static class AuthEndpoints
{
    private sealed record LoginDto(string Username, string Password);
    private sealed record LoginResult(string Token, string Role, int? EvaluadorId);

    public static RouteGroupBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (LoginDto dto, IJwtTokenService jwt, appEvaluaciones.Web.Services.ISqlConnectionFactory factory, CancellationToken ct) =>
        {
            using IDbConnection db = factory.Create();
            const string sql = @"SELECT TOP 1 UsuarioId, Usuario as Username, PasswordHash, Rol, EvaluadorId
FROM dbo.Usuarios WHERE Usuario = @u AND (Activo = 1 OR Activo IS NULL)";
            var row = await db.QueryFirstOrDefaultAsync<(int UsuarioId, string Username, byte[]? PasswordHash, string Rol, int? EvaluadorId)>(new CommandDefinition(sql, new { u = dto.Username }, cancellationToken: ct));
            if (row.Equals(default((int, string, byte[]?, string, int?))))
                return Results.Unauthorized();

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(dto.Password));
            if (row.PasswordHash is null || !hash.SequenceEqual(row.PasswordHash))
                return Results.Unauthorized();

            var token = jwt.CreateToken(row.Username, row.Rol, row.EvaluadorId);
            return Results.Ok(new LoginResult(token, row.Rol, row.EvaluadorId));
        });

        return group;
    }
}
