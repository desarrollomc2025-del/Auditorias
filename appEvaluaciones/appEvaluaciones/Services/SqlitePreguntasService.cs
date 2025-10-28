using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Maui.Storage;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Services;

public sealed class SqlitePreguntasService : IPreguntasService
{
    private readonly string _dbPath;

    public SqlitePreguntasService()
    {
        var folder = FileSystem.AppDataDirectory;
        _dbPath = Path.Combine(folder, "auditorias_offline.db");
        Initialize();
    }

    private void Initialize()
    {
        using var conn = Create();
        conn.Execute(@"CREATE TABLE IF NOT EXISTS Preguntas (
            PreguntaId INTEGER PRIMARY KEY,
            CategoriaId INTEGER NOT NULL,
            Codigo TEXT NOT NULL,
            TextoPregunta TEXT NOT NULL,
            Ponderacion REAL NOT NULL,
            Orden INTEGER NOT NULL,
            Activo INTEGER NOT NULL,
            FechaCreacion TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_Preguntas_Categoria ON Preguntas(CategoriaId);
        ");
    }

    private IDbConnection Create() => new SqliteConnection($"Data Source={_dbPath};Cache=Shared");

    public async Task<IReadOnlyList<Pregunta>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = Create();
        var list = await conn.QueryAsync<Pregunta>(new CommandDefinition(
            "SELECT PreguntaId, CategoriaId, Codigo, TextoPregunta, Ponderacion, Orden, Activo, FechaCreacion FROM Preguntas WHERE Activo = 1",
            cancellationToken: ct));
        return list.ToList();
    }

    public async Task<Pregunta?> GetByIdAsync(int preguntaId, CancellationToken ct = default)
    {
        using var conn = Create();
        return await conn.QuerySingleOrDefaultAsync<Pregunta>(new CommandDefinition(
            "SELECT PreguntaId, CategoriaId, Codigo, TextoPregunta, Ponderacion, Orden, Activo, FechaCreacion FROM Preguntas WHERE PreguntaId = @preguntaId",
            new { preguntaId }, cancellationToken: ct));
    }

    public async Task<int> UpsertAsync(Pregunta pregunta, CancellationToken ct = default)
    {
        using var conn = Create();
        const string sql = @"INSERT INTO Preguntas(PreguntaId, CategoriaId, Codigo, TextoPregunta, Ponderacion, Orden, Activo, FechaCreacion)
VALUES(@PreguntaId, @CategoriaId, @Codigo, @TextoPregunta, @Ponderacion, @Orden, @Activo, @FechaCreacion)
ON CONFLICT(PreguntaId) DO UPDATE SET 
  CategoriaId=excluded.CategoriaId,
  Codigo=excluded.Codigo,
  TextoPregunta=excluded.TextoPregunta,
  Ponderacion=excluded.Ponderacion,
  Orden=excluded.Orden,
  Activo=excluded.Activo";
        await conn.ExecuteAsync(new CommandDefinition(sql, pregunta, cancellationToken: ct));
        return pregunta.PreguntaId;
    }

    public async Task DeleteAsync(int preguntaId, CancellationToken ct = default)
    {
        using var conn = Create();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Preguntas SET Activo = 0 WHERE PreguntaId = @preguntaId",
            new { preguntaId }, cancellationToken: ct));
    }
}

