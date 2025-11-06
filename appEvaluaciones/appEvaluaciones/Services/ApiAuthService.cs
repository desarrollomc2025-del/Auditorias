using System.Net.Http.Json;
using System.Net.Http.Headers;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class ApiAuthService(HttpClient http) : IAuthService
{
    private AuthState _state = new(false, null, null, null);
    public AuthState Current => _state;

    public async Task<AuthState> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("api/auth/login", new { username, password }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<LoginPayload>(cancellationToken: ct);
        if (json is null || string.IsNullOrWhiteSpace(json.Token))
            throw new InvalidOperationException("Login sin token");

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.Token);
        _state = new AuthState(true, username, json.Role, json.EvaluadorId);
        return _state;
    }

    public Task LogoutAsync()
    {
        http.DefaultRequestHeaders.Authorization = null;
        _state = new AuthState(false, null, null, null);
        return Task.CompletedTask;
    }

    private sealed class LoginPayload
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? EvaluadorId { get; set; }
    }
}

