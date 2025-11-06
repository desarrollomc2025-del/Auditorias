using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using appEvaluaciones.Shared.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace appEvaluaciones.Web.Services;

public sealed class WebAuthService(ISqlConnectionFactory factory, IHttpContextAccessor httpAccessor) : IAuthService
{
    public AuthState Current
    {
        get
        {
            var user = httpAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return new AuthState(false, null, null, null);
            var role = user.FindFirstValue(ClaimTypes.Role);
            var evalidStr = user.FindFirstValue("evalid");
            int? evalid = int.TryParse(evalidStr, out var v) ? v : null;
            return new AuthState(true, user.Identity?.Name, role, evalid);
        }
    }

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

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, row.Usuario),
            new(ClaimTypes.Role, row.Rol)
        };
        if (row.EvaluadorId.HasValue)
            claims.Add(new Claim("evalid", row.EvaluadorId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };
        await httpAccessor.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
        return Current;
    }

    public Task LogoutAsync()
    {
        return httpAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
