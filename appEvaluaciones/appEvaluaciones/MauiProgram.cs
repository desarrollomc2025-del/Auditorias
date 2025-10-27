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
        builder.Services.AddSingleton(new HttpClient
        {
#if ANDROID
            // Maps to host machine from Android emulator
            BaseAddress = new Uri("http://10.0.2.2:5098/")
#elif WINDOWS
            BaseAddress = new Uri("http://localhost:5098/")
#else
            BaseAddress = new Uri("http://localhost:5098/")
#endif
        });

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

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
