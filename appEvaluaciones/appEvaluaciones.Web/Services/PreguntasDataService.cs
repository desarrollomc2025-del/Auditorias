using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Services;

public sealed class PreguntasDataService(ISqlConnectionFactory factory) : IPreguntasService
{
    public async Task<IReadOnlyList<Pregunta>> GetAllAsync(CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        var rows = await db.QueryAsync<Pregunta>(new CommandDefinition(
            "SELECT PreguntaId, CategoriaId, Codigo, TextoPregunta, Ponderacion, Orden, Activo, FechaCreacion FROM dbo.Preguntas WHERE Activo = 1",
            cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Pregunta?> GetByIdAsync(int preguntaId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        return await db.QuerySingleOrDefaultAsync<Pregunta>(new CommandDefinition(
            "SELECT PreguntaId, CategoriaId, Codigo, TextoPregunta, Ponderacion, Orden, Activo, FechaCreacion FROM dbo.Preguntas WHERE PreguntaId = @preguntaId",
            new { preguntaId }, cancellationToken: ct));
    }

    public async Task<int> UpsertAsync(Pregunta pregunta, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Preguntas WHERE PreguntaId=@PreguntaId)
BEGIN
    UPDATE dbo.Preguntas SET CategoriaId=@CategoriaId, Codigo=@Codigo, TextoPregunta=@TextoPregunta, Ponderacion=@Ponderacion, Orden=@Orden, Activo=@Activo
    WHERE PreguntaId=@PreguntaId;
    SELECT @PreguntaId;
END
ELSE
BEGIN
    INSERT INTO dbo.Preguntas(CategoriaId, Codigo, TextoPregunta, Ponderacion, Orden, Activo, FechaCreacion)
    VALUES(@CategoriaId, @Codigo, @TextoPregunta, @Ponderacion, @Orden, @Activo, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, pregunta, cancellationToken: ct));
    }

    public async Task DeleteAsync(int preguntaId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Preguntas SET Activo = 0 WHERE PreguntaId = @preguntaId",
            new { preguntaId }, cancellationToken: ct));
    }
}

