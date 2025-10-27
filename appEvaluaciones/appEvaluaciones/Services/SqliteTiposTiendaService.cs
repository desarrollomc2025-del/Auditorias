using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Maui.Storage;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class SqliteTiposTiendaService : ITiposTiendaService
{
    private readonly string _dbPath;

    public SqliteTiposTiendaService()
    {
        var folder = FileSystem.AppDataDirectory;
        _dbPath = Path.Combine(folder, "auditorias_offline.db");
        Initialize();
    }

    private void Initialize()
    {
        using var conn = Create();
        conn.Execute(@"CREATE TABLE IF NOT EXISTS TiposTienda (
            TipoTiendaId INTEGER PRIMARY KEY,
            Nombre TEXT NOT NULL,
            Descripcion TEXT NULL,
            Activo INTEGER NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UQ_TiposTienda_Nombre ON TiposTienda(Nombre);
        ");
        // Semillas mínimas si está vacío
        var count = conn.ExecuteScalar<long>("SELECT COUNT(1) FROM TiposTienda;");
        if (count == 0)
        {
            conn.Execute("INSERT INTO TiposTienda (TipoTiendaId, Nombre, Descripcion, Activo) VALUES (1,'Clasica','Tienda tradicional con surtido completo',1),(2,'Express','Tienda de conveniencia o formato reducido',1);");
        }
    }

    private IDbConnection Create() => new SqliteConnection($"Data Source={_dbPath};Cache=Shared");

    public async Task<IReadOnlyList<TipoTienda>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = Create();
        var list = await conn.QueryAsync<TipoTienda>(new CommandDefinition(
            "SELECT TipoTiendaId, Nombre, Descripcion, Activo FROM TiposTienda WHERE Activo = 1",
            cancellationToken: ct));
        return list.ToList();
    }
}

