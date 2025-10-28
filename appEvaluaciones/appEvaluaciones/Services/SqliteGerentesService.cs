using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Maui.Storage;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class SqliteGerentesService : IGerentesService
{
    private readonly string _dbPath;

    public SqliteGerentesService()
    {
        var folder = FileSystem.AppDataDirectory;
        _dbPath = Path.Combine(folder, "auditorias_offline.db");
        Initialize();
    }

    private void Initialize()
    {
        using var conn = Create();
        conn.Execute(@"CREATE TABLE IF NOT EXISTS Gerentes (
            GerenteId INTEGER PRIMARY KEY,
            Codigo TEXT NOT NULL,
            Nombre TEXT NOT NULL,
            Rol TEXT NOT NULL,
            GerenteRegionalId INTEGER NULL,
            Activo INTEGER NOT NULL,
            FechaCreacion TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UQ_Gerentes_Codigo ON Gerentes(Codigo);
        ");
    }

    private IDbConnection Create() => new SqliteConnection($"Data Source={_dbPath};Cache=Shared");

    public async Task<IReadOnlyList<Gerente>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = Create();
        var list = await conn.QueryAsync<Gerente>(new CommandDefinition(
            "SELECT GerenteId, Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion FROM Gerentes WHERE Activo = 1",
            cancellationToken: ct));
        return list.ToList();
    }

    public async Task<Gerente?> GetByIdAsync(int gerenteId, CancellationToken ct = default)
    {
        using var conn = Create();
        return await conn.QuerySingleOrDefaultAsync<Gerente>(new CommandDefinition(
            "SELECT GerenteId, Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion FROM Gerentes WHERE GerenteId = @gerenteId",
            new { gerenteId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Gerente>> GetRegionalesAsync(CancellationToken ct = default)
    {
        using var conn = Create();
        var list = await conn.QueryAsync<Gerente>(new CommandDefinition(
            "SELECT GerenteId, Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion FROM Gerentes WHERE Activo = 1 AND Rol = 'Regional'",
            cancellationToken: ct));
        return list.ToList();
    }

    public async Task<int> UpsertAsync(Gerente gerente, CancellationToken ct = default)
    {
        if (string.Equals(gerente.Rol, "Regional", StringComparison.OrdinalIgnoreCase))
            gerente.GerenteRegionalId = null;

        using var conn = Create();
        const string sql = @"INSERT INTO Gerentes(GerenteId, Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion)
VALUES(@GerenteId, @Codigo, @Nombre, @Rol, @GerenteRegionalId, @Activo, @FechaCreacion)
ON CONFLICT(GerenteId) DO UPDATE SET 
  Codigo=excluded.Codigo,
  Nombre=excluded.Nombre,
  Rol=excluded.Rol,
  GerenteRegionalId=excluded.GerenteRegionalId,
  Activo=excluded.Activo";
        await conn.ExecuteAsync(new CommandDefinition(sql, gerente, cancellationToken: ct));
        return gerente.GerenteId;
    }

    public async Task DeleteAsync(int gerenteId, CancellationToken ct = default)
    {
        using var conn = Create();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Gerentes SET Activo = 0 WHERE GerenteId = @gerenteId",
            new { gerenteId }, cancellationToken: ct));
    }
}

