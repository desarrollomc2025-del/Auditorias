using appEvaluaciones.Web.Components;
using appEvaluaciones.Shared.Services;
using appEvaluaciones.Web.Services;
using appEvaluaciones.Web.Endpoints;
using appEvaluaciones.Web.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// Configure JWT (must be before Build())
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
if (jwtSection is null || string.IsNullOrWhiteSpace(jwtSection.Key))
{
    // Provide a fallback dev key to avoid misconfig at dev time
    jwtSection = new JwtOptions { Issuer = "appEvaluaciones", Audience = "appEvaluaciones", Key = "dev-secret-key-change-me-please-1234567890" };
}
var key = Encoding.UTF8.GetBytes(jwtSection.Key);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    options.AddPolicy("Evaluador", p => p.RequireRole("Evaluador", "Admin"));
    options.AddPolicy("Admin", p => p.RequireRole("Admin"));
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

app.Run();
