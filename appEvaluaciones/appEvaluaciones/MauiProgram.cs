using Microsoft.Extensions.Logging;
using appEvaluaciones.Shared.Services;
using appEvaluaciones.Services;
using System.Net.Http;
using Microsoft.Maui.Networking;

namespace appEvaluaciones;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add device-specific services used by the appEvaluaciones.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

        builder.Services.AddMauiBlazorWebView();

        // HTTP client to Web backend (adjust BaseAddress to your server)
#if ANDROID
        // Android emulator reaches the host via 10.0.2.2. Use HTTPS dev port.
        // In DEBUG, relax cert validation only for development convenience.
        HttpClientHandler androidHandler = new();
#if DEBUG
        androidHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
        builder.Services.AddSingleton(new HttpClient(androidHandler)
        {
            BaseAddress = new Uri("https://10.0.2.2:7144/")
        });
#elif WINDOWS
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7144/")
        });
#else
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7144/")
        });
#endif

        // Essentials: IConnectivity
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);

        // Data services: online (API) + offline (SQLite) via proxy
        builder.Services.AddSingleton<ITiendasService>(sp =>
        {
            var http = sp.GetRequiredService<HttpClient>();
            var connectivity = sp.GetRequiredService<IConnectivity>();
            var online = new ApiTiendasService(http);
            var offline = new SqliteTiendasService();
            return new TiendasServiceProxy(online, offline, connectivity);
        });

        builder.Services.AddSingleton<IEmpresasService>(sp =>
        {
            var http = sp.GetRequiredService<HttpClient>();
            var connectivity = sp.GetRequiredService<IConnectivity>();
            var online = new ApiEmpresasService(http);
            var offline = new SqliteEmpresasService();
            return new EmpresasServiceProxy(online, offline, connectivity);
        });

        builder.Services.AddSingleton<ITiposTiendaService>(sp =>
        {
            var http = sp.GetRequiredService<HttpClient>();
            var connectivity = sp.GetRequiredService<IConnectivity>();
            var online = new ApiTiposTiendaService(http);
            var offline = new SqliteTiposTiendaService();
            return new TiposTiendaServiceProxy(online, offline, connectivity);
        });

        builder.Services.AddSingleton<IEvaluadoresService>(sp =>
        {
            var http = sp.GetRequiredService<HttpClient>();
            var connectivity = sp.GetRequiredService<IConnectivity>();
            var online = new ApiEvaluadoresService(http);
            var offline = new SqliteEvaluadoresService();
            return new EvaluadoresServiceProxy(online, offline, connectivity);
        });

        builder.Services.AddSingleton<IGerentesService>(sp =>
        {
            var http = sp.GetRequiredService<HttpClient>();
            var connectivity = sp.GetRequiredService<IConnectivity>();
            var online = new ApiGerentesService(http);
            var offline = new SqliteGerentesService();
            return new GerentesServiceProxy(online, offline, connectivity);
        });

        builder.Services.AddSingleton<ICategoriasService>(sp =>
        {
            var http = sp.GetRequiredService<HttpClient>();
            var connectivity = sp.GetRequiredService<IConnectivity>();
            var online = new ApiCategoriasService(http);
            var offline = new SqliteCategoriasService();
            return new CategoriasServiceProxy(online, offline, connectivity);
        });

        builder.Services.AddSingleton<IPreguntasService>(sp =>
        {
            var http = sp.GetRequiredService<HttpClient>();
            var connectivity = sp.GetRequiredService<IConnectivity>();
            var online = new ApiPreguntasService(http);
            var offline = new SqlitePreguntasService();
            return new PreguntasServiceProxy(online, offline, connectivity);
        });

        // Evidencias: solo online (API)
        builder.Services.AddSingleton<IEvidenciasService>(sp =>
        {
            var http = sp.GetRequiredService<HttpClient>();
            return new ApiEvidenciasService(http);
        });

        // Evaluaciones: online (API)
        builder.Services.AddSingleton<IEvaluacionesService>(sp =>
        {
            var http = sp.GetRequiredService<HttpClient>();
            return new ApiEvaluacionesService(http);
        });

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}


