using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Services;

public sealed class WebAuthService(ISqlConnectionFactory factory) : IAuthService
{
    private AuthState _state = new(false, null, null, null);
    public AuthState Current => _state;

    public async Task<AuthState> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"SELECT TOP 1 UsuarioId, Usuario, PasswordHash, Rol, EvaluadorId, COALESCE(Activo,1) AS Activo
FROM dbo.Usuarios WHERE Usuario = @u";
        var row = await db.QueryFirstOrDefaultAsync<(int UsuarioId, string Usuario, byte[]? PasswordHash, string Rol, int? EvaluadorId, int Activo)>(new CommandDefinition(sql, new { u = username }, cancellationToken: ct));
        if (row.Equals(default((int, string, byte[]?, string, int?, int))))
            throw new InvalidOperationException("Usuario o contraseña incorrectos");
        if (row.Activo == 0)
            throw new InvalidOperationException("Usuario inactivo");

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        if (row.PasswordHash is null || !hash.SequenceEqual(row.PasswordHash))
            throw new InvalidOperationException("Usuario o contraseña incorrectos");

        _state = new AuthState(true, row.Usuario, row.Rol, row.EvaluadorId);
        return _state;
    }

    public Task LogoutAsync()
    {
        _state = new AuthState(false, null, null, null);
        return Task.CompletedTask;
    }
}

