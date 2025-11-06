namespace appEvaluaciones.Shared.Services;

public sealed record AuthState(bool IsAuthenticated, string? Username, string? Role, int? EvaluadorId);

public interface IAuthService
{
    Task<AuthState> LoginAsync(string username, string password, CancellationToken ct = default);
    Task LogoutAsync();
    AuthState Current { get; }
}

