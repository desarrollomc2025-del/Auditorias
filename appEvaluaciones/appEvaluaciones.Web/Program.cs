using appEvaluaciones.Web.Components;
using appEvaluaciones.Shared.Services;
using appEvaluaciones.Web.Services;
using appEvaluaciones.Web.Endpoints;
using appEvaluaciones.Web.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add device-specific services used by the appEvaluaciones.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<ITiendasService, TiendasDataService>();
builder.Services.AddScoped<IEmpresasService, EmpresasDataService>();
builder.Services.AddScoped<ITiposTiendaService, TiposTiendaDataService>();
builder.Services.AddScoped<IEvaluadoresService, EvaluadoresDataService>();
builder.Services.AddScoped<IGerentesService, GerentesDataService>();
builder.Services.AddScoped<ICategoriasService, CategoriasDataService>();
builder.Services.AddScoped<IPreguntasService, PreguntasDataService>();
builder.Services.AddScoped<IEvidenciasService, EvidenciasDataService>();
builder.Services.AddScoped<IEvaluacionesService, EvaluacionesDataService>();
builder.Services.AddScoped<IAuthService, WebAuthService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// Configure Cookies + JWT (must be before Build())
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
if (jwtSection is null || string.IsNullOrWhiteSpace(jwtSection.Key))
{
    // Provide a fallback dev key to avoid misconfig at dev time
    jwtSection = new JwtOptions { Issuer = "appEvaluaciones", Audience = "appEvaluaciones", Key = "dev-secret-key-change-me-please-1234567890" };
}
var key = Encoding.UTF8.GetBytes(jwtSection.Key);
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.SlidingExpiration = true;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection.Issuer,
            ValidAudience = jwtSection.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Evaluador", p =>
    {
        p.RequireRole("Evaluador", "Admin");
        p.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
    });
    options.AddPolicy("Admin", p =>
    {
        p.RequireRole("Admin");
        p.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");


app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(appEvaluaciones.Shared._Imports).Assembly);

// Minimal APIs
app.MapAuth();
app.MapTiendas().RequireAuthorization("Admin");
app.MapEmpresas().RequireAuthorization("Admin");
app.MapTiposTienda().RequireAuthorization("Admin");
app.MapEvaluadores().RequireAuthorization("Admin");
app.MapGerentes().RequireAuthorization("Admin");
app.MapCategorias().RequireAuthorization("Admin");
app.MapPreguntas().RequireAuthorization("Evaluador");
app.MapEvidencias().RequireAuthorization("Evaluador");
app.MapEvaluaciones().RequireAuthorization("Evaluador");
app.MapUsuarios().RequireAuthorization("Admin");

// Cookie-based login/logout endpoints for Blazor Server UI
app.MapPost("/auth/cookie-login", async (HttpContext ctx, ISqlConnectionFactory factory) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    using var db = factory.Create();
    const string sql = @"SELECT TOP 1 UsuarioId, Usuario, PasswordHash, Rol, EvaluadorId, COALESCE(Activo,1) AS Activo FROM dbo.Usuarios WHERE Usuario=@u";
    var row = await db.QueryFirstOrDefaultAsync<(int UsuarioId, string Usuario, byte[]? PasswordHash, string Rol, int? EvaluadorId, int Activo)>(sql, new { u = username });
    if (row.Equals(default((int, string, byte[]?, string, int?, int))) || row.Activo == 0)
        return Results.Redirect("/login?e=1");
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
    if (row.PasswordHash is null || !hash.SequenceEqual(row.PasswordHash))
        return Results.Redirect("/login?e=1");

    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.Name, row.Usuario),
        new(System.Security.Claims.ClaimTypes.Role, row.Rol)
    };
    if (row.EvaluadorId.HasValue)
        claims.Add(new System.Security.Claims.Claim("evalid", row.EvaluadorId.Value.ToString()));
    var id = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new System.Security.Claims.ClaimsPrincipal(id), new Microsoft.AspNetCore.Authentication.AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
    return Results.Redirect("/");
});

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.Run();
