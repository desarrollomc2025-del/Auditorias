using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;
using Microsoft.Maui.Storage;

namespace appEvaluaciones.Services;

public sealed class SqliteTiendasService : ITiendasService
{
    private readonly string _dbPath;

    public SqliteTiendasService()
    {
        var folder = FileSystem.AppDataDirectory;
        _dbPath = Path.Combine(folder, "auditorias_offline.db");
        Initialize();
    }

    private void Initialize()
    {
        using var conn = Create();
        conn.Execute(@"CREATE TABLE IF NOT EXISTS Tiendas (
            TiendaId INTEGER PRIMARY KEY,
            EmpresaId INTEGER NOT NULL,
            Codigo TEXT NOT NULL,
            CodigoInterno TEXT NULL,
            Descripcion TEXT NOT NULL,
            TipoTiendaId INTEGER NOT NULL DEFAULT 1,
            Latitud REAL NULL,
            Longitud REAL NULL,
            Eliminado INTEGER NOT NULL,
            FechaCreacion TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_Tiendas_Codigo ON Tiendas(Codigo);
        ");

        // Intentar migrar si falta la columna TipoTiendaId (SQLite no tiene IF NOT EXISTS en ADD COLUMN)
        try { conn.Execute("ALTER TABLE Tiendas ADD COLUMN TipoTiendaId INTEGER NOT NULL DEFAULT 1;"); } catch { /* ignore if exists */ }
    }

    private IDbConnection Create()
        => new SqliteConnection($"Data Source={_dbPath};Cache=Shared");

    public async Task<IReadOnlyList<Tienda>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = Create();
        var list = await conn.QueryAsync<Tienda>(new CommandDefinition(
            "SELECT TiendaId, EmpresaId, Codigo, CodigoInterno, Descripcion, Latitud, Longitud, Eliminado, FechaCreacion FROM Tiendas WHERE Eliminado = 0",
            cancellationToken: ct));
        return list.ToList();
    }

    public async Task<Tienda?> GetByIdAsync(int tiendaId, CancellationToken ct = default)
    {
        using var conn = Create();
        return await conn.QuerySingleOrDefaultAsync<Tienda>(new CommandDefinition(
            "SELECT TiendaId, EmpresaId, Codigo, CodigoInterno, Descripcion, Latitud, Longitud, Eliminado, FechaCreacion FROM Tiendas WHERE TiendaId = @tiendaId",
            new { tiendaId }, cancellationToken: ct));
    }

    public async Task<int> UpsertAsync(Tienda tienda, CancellationToken ct = default)
    {
        using var conn = Create();
        const string sql = @"INSERT INTO Tiendas(TiendaId, EmpresaId, Codigo, CodigoInterno, Descripcion, TipoTiendaId, Latitud, Longitud, Eliminado, FechaCreacion)
VALUES(@TiendaId, @EmpresaId, @Codigo, @CodigoInterno, @Descripcion, @TipoTiendaId, @Latitud, @Longitud, @Eliminado, @FechaCreacion)
ON CONFLICT(TiendaId) DO UPDATE SET 
  EmpresaId=excluded.EmpresaId,
  Codigo=excluded.Codigo,
  CodigoInterno=excluded.CodigoInterno,
  Descripcion=excluded.Descripcion,
  TipoTiendaId=excluded.TipoTiendaId,
  Latitud=excluded.Latitud,
  Longitud=excluded.Longitud,
  Eliminado=excluded.Eliminado";
        await conn.ExecuteAsync(new CommandDefinition(sql, tienda, cancellationToken: ct));
        return tienda.TiendaId;
    }

    public async Task DeleteAsync(int tiendaId, CancellationToken ct = default)
    {
        using var conn = Create();
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Tiendas WHERE TiendaId = @tiendaId",
            new { tiendaId }, cancellationToken: ct));
    }
}
