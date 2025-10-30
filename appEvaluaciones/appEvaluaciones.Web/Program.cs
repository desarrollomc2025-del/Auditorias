using appEvaluaciones.Web.Components;
using appEvaluaciones.Shared.Services;
using appEvaluaciones.Web.Services;
using appEvaluaciones.Web.Endpoints;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(appEvaluaciones.Shared._Imports).Assembly);

// Minimal APIs
app.MapTiendas();
app.MapEmpresas();
app.MapTiposTienda();
app.MapEvaluadores();
app.MapGerentes();
app.MapCategorias();
app.MapPreguntas();
app.MapEvidencias();
app.MapEvaluaciones();

app.Run();


