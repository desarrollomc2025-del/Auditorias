using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Maui.Storage;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class SqliteEvaluadoresService : IEvaluadoresService
{
    private readonly string _dbPath;

    public SqliteEvaluadoresService()
    {
        var folder = FileSystem.AppDataDirectory;
        _dbPath = Path.Combine(folder, "auditorias_offline.db");
        Initialize();
    }

    private void Initialize()
    {
        using var conn = Create();
        conn.Execute(@"CREATE TABLE IF NOT EXISTS Evaluadores (
            EvaluadorId INTEGER PRIMARY KEY,
            Codigo TEXT NOT NULL,
            Nombre TEXT NOT NULL,
            Activo INTEGER NOT NULL,
            FechaCreacion TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UQ_Evaluadores_Codigo ON Evaluadores(Codigo);
        ");
    }

    private IDbConnection Create() => new SqliteConnection($"Data Source={_dbPath};Cache=Shared");

    public async Task<IReadOnlyList<Evaluador>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = Create();
        var list = await conn.QueryAsync<Evaluador>(new CommandDefinition(
            "SELECT EvaluadorId, Codigo, Nombre, Activo, FechaCreacion FROM Evaluadores WHERE Activo = 1",
            cancellationToken: ct));
        return list.ToList();
    }

    public async Task<Evaluador?> GetByIdAsync(int evaluadorId, CancellationToken ct = default)
    {
        using var conn = Create();
        return await conn.QuerySingleOrDefaultAsync<Evaluador>(new CommandDefinition(
            "SELECT EvaluadorId, Codigo, Nombre, Activo, FechaCreacion FROM Evaluadores WHERE EvaluadorId = @evaluadorId",
            new { evaluadorId }, cancellationToken: ct));
    }

    public async Task<int> UpsertAsync(Evaluador evaluador, CancellationToken ct = default)
    {
        using var conn = Create();
        const string sql = @"INSERT INTO Evaluadores(EvaluadorId, Codigo, Nombre, Activo, FechaCreacion)
VALUES(@EvaluadorId, @Codigo, @Nombre, @Activo, @FechaCreacion)
ON CONFLICT(EvaluadorId) DO UPDATE SET 
  Codigo=excluded.Codigo,
  Nombre=excluded.Nombre,
  Activo=excluded.Activo";
        await conn.ExecuteAsync(new CommandDefinition(sql, evaluador, cancellationToken: ct));
        return evaluador.EvaluadorId;
    }

    public async Task DeleteAsync(int evaluadorId, CancellationToken ct = default)
    {
        using var conn = Create();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Evaluadores SET Activo = 0 WHERE EvaluadorId = @evaluadorId",
            new { evaluadorId }, cancellationToken: ct));
    }
}

