using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Maui.Storage;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class SqliteCategoriasService : ICategoriasService
{
    private readonly string _dbPath;

    public SqliteCategoriasService()
    {
        var folder = FileSystem.AppDataDirectory;
        _dbPath = Path.Combine(folder, "auditorias_offline.db");
        Initialize();
    }

    private void Initialize()
    {
        using var conn = Create();
        conn.Execute(@"CREATE TABLE IF NOT EXISTS Categorias (
            CategoriaId INTEGER PRIMARY KEY,
            Codigo TEXT NOT NULL,
            Descripcion TEXT NOT NULL,
            Ponderacion REAL NOT NULL,
            Activo INTEGER NOT NULL,
            FechaCreacion TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UQ_Categorias_Codigo ON Categorias(Codigo);
        ");
    }

    private IDbConnection Create() => new SqliteConnection($"Data Source={_dbPath};Cache=Shared");

    public async Task<IReadOnlyList<Categoria>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = Create();
        var list = await conn.QueryAsync<Categoria>(new CommandDefinition(
            "SELECT CategoriaId, Codigo, Descripcion, Ponderacion, Activo, FechaCreacion FROM Categorias WHERE Activo = 1",
            cancellationToken: ct));
        return list.ToList();
    }

    public async Task<Categoria?> GetByIdAsync(int categoriaId, CancellationToken ct = default)
    {
        using var conn = Create();
        return await conn.QuerySingleOrDefaultAsync<Categoria>(new CommandDefinition(
            "SELECT CategoriaId, Codigo, Descripcion, Ponderacion, Activo, FechaCreacion FROM Categorias WHERE CategoriaId = @categoriaId",
            new { categoriaId }, cancellationToken: ct));
    }

    public async Task<int> UpsertAsync(Categoria categoria, CancellationToken ct = default)
    {
        using var conn = Create();
        const string sql = @"INSERT INTO Categorias(CategoriaId, Codigo, Descripcion, Ponderacion, Activo, FechaCreacion)
VALUES(@CategoriaId, @Codigo, @Descripcion, @Ponderacion, @Activo, @FechaCreacion)
ON CONFLICT(CategoriaId) DO UPDATE SET 
  Codigo=excluded.Codigo,
  Descripcion=excluded.Descripcion,
  Ponderacion=excluded.Ponderacion,
  Activo=excluded.Activo";
        await conn.ExecuteAsync(new CommandDefinition(sql, categoria, cancellationToken: ct));
        return categoria.CategoriaId;
    }

    public async Task DeleteAsync(int categoriaId, CancellationToken ct = default)
    {
        using var conn = Create();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Categorias SET Activo = 0 WHERE CategoriaId = @categoriaId",
            new { categoriaId }, cancellationToken: ct));
    }
}

