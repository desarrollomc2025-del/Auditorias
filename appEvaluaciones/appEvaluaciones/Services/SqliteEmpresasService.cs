using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Maui.Storage;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class SqliteEmpresasService : IEmpresasService
{
    private readonly string _dbPath;

    public SqliteEmpresasService()
    {
        var folder = FileSystem.AppDataDirectory;
        _dbPath = Path.Combine(folder, "auditorias_offline.db");
        Initialize();
    }

    private void Initialize()
    {
        using var conn = Create();
        conn.Execute(@"CREATE TABLE IF NOT EXISTS Empresas (
            EmpresaId INTEGER PRIMARY KEY,
            Codigo TEXT NOT NULL,
            Nombre TEXT NOT NULL,
            Direccion TEXT NULL,
            Eliminado INTEGER NOT NULL,
            FechaCreacion TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UQ_Empresas_Codigo ON Empresas(Codigo);
        ");
    }

    private IDbConnection Create()
        => new SqliteConnection($"Data Source={_dbPath};Cache=Shared");

    public async Task<IReadOnlyList<Empresa>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = Create();
        var list = await conn.QueryAsync<Empresa>(new CommandDefinition(
            "SELECT EmpresaId, Codigo, Nombre, Direccion, Eliminado, FechaCreacion FROM Empresas WHERE Eliminado = 0",
            cancellationToken: ct));
        return list.ToList();
    }

    public async Task<Empresa?> GetByIdAsync(int empresaId, CancellationToken ct = default)
    {
        using var conn = Create();
        return await conn.QuerySingleOrDefaultAsync<Empresa>(new CommandDefinition(
            "SELECT EmpresaId, Codigo, Nombre, Direccion, Eliminado, FechaCreacion FROM Empresas WHERE EmpresaId = @empresaId",
            new { empresaId }, cancellationToken: ct));
    }

    public async Task<int> UpsertAsync(Empresa empresa, CancellationToken ct = default)
    {
        using var conn = Create();
        const string sql = @"INSERT INTO Empresas(EmpresaId, Codigo, Nombre, Direccion, Eliminado, FechaCreacion)
VALUES(@EmpresaId, @Codigo, @Nombre, @Direccion, @Eliminado, @FechaCreacion)
ON CONFLICT(EmpresaId) DO UPDATE SET 
  Codigo=excluded.Codigo,
  Nombre=excluded.Nombre,
  Direccion=excluded.Direccion,
  Eliminado=excluded.Eliminado";
        await conn.ExecuteAsync(new CommandDefinition(sql, empresa, cancellationToken: ct));
        return empresa.EmpresaId;
    }

    public async Task DeleteAsync(int empresaId, CancellationToken ct = default)
    {
        using var conn = Create();
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Empresas WHERE EmpresaId = @empresaId",
            new { empresaId }, cancellationToken: ct));
    }
}

