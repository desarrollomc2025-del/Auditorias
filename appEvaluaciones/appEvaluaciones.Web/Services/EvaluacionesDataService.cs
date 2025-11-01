using System.Data;
using Dapper;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Services;

public sealed class EvaluacionesDataService(ISqlConnectionFactory factory) : IEvaluacionesService
{
    public async Task<int> CreateAsync(int tiendaId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"INSERT INTO dbo.Evaluaciones(TiendaId, FechaCreacion)
VALUES(@tiendaId, SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { tiendaId }, cancellationToken: ct, commandTimeout: 60));
    }

    public async Task UpsertDetalleAsync(int evaluacionId, int preguntaId, bool? respuesta, string? comentario, decimal ponderacion, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"IF NOT EXISTS(SELECT 1 FROM dbo.Evaluaciones WHERE EvaluacionId=@evaluacionId)
BEGIN
    THROW 50000, 'Evaluacion no existe', 1;
END

IF EXISTS(SELECT 1 FROM dbo.DetalleEvaluaciones WHERE EvaluacionId=@evaluacionId AND PreguntaId=@preguntaId)
BEGIN
    UPDATE dbo.DetalleEvaluaciones
    SET Respuesta=@respuesta, Comentario=@comentario, Ponderacion=@ponderacion
    WHERE EvaluacionId=@evaluacionId AND PreguntaId=@preguntaId;
END
ELSE
BEGIN
    INSERT INTO dbo.DetalleEvaluaciones(EvaluacionId, PreguntaId, Respuesta, Comentario, Ponderacion)
    VALUES(@evaluacionId, @preguntaId, @respuesta, @comentario, @ponderacion);
END";
        await db.ExecuteAsync(new CommandDefinition(sql, new { evaluacionId, preguntaId, respuesta, comentario, ponderacion }, cancellationToken: ct, commandTimeout: 60));
    }

    public async Task UpsertDetallesAsync(int evaluacionId, IEnumerable<DetalleUpsert> detalles, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"SET XACT_ABORT ON;
IF NOT EXISTS(SELECT 1 FROM dbo.Evaluaciones WHERE EvaluacionId=@evaluacionId) THROW 50000, 'Evaluacion no existe', 1;

DECLARE @json NVARCHAR(MAX) = @payload;

;WITH src AS (
    SELECT * FROM OPENJSON(@json)
    WITH (
        PreguntaId   INT            '$.PreguntaId',
        Respuesta    BIT            '$.Respuesta',
        Comentario   NVARCHAR(500)  '$.Comentario',
        Ponderacion  DECIMAL(10,2)  '$.Ponderacion'
    )
)
MERGE dbo.DetalleEvaluaciones AS d
USING src AS s
ON d.EvaluacionId = @evaluacionId AND d.PreguntaId = s.PreguntaId
WHEN MATCHED THEN
    UPDATE SET Respuesta = s.Respuesta, Comentario = s.Comentario, Ponderacion = s.Ponderacion
WHEN NOT MATCHED THEN
    INSERT (EvaluacionId, PreguntaId, Respuesta, Comentario, Ponderacion)
    VALUES (@evaluacionId, s.PreguntaId, s.Respuesta, s.Comentario, s.Ponderacion);";

        var payload = System.Text.Json.JsonSerializer.Serialize(detalles);
        var args = new { evaluacionId, payload };
        await db.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct, commandTimeout: 120));
    }

    public async Task FinalizarAsync(int evaluacionId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"UPDATE dbo.Evaluaciones
SET Estado = 'Finalizada', FechaCierre = SYSUTCDATETIME()
WHERE EvaluacionId=@evaluacionId;";
        await db.ExecuteAsync(new CommandDefinition(sql, new { evaluacionId }, cancellationToken: ct, commandTimeout: 30));
    }

    public async Task<appEvaluaciones.Shared.Models.EvaluacionVm> GetAsync(int evaluacionId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"SELECT TOP 1 EvaluacionId, TiendaId, FechaCreacion
FROM dbo.Evaluaciones WHERE EvaluacionId=@id;
SELECT PreguntaId, Respuesta, Comentario, Ponderacion
FROM dbo.DetalleEvaluaciones d
JOIN dbo.Evaluaciones e ON e.EvaluacionId = d.EvaluacionId
WHERE e.EvaluacionId=@id
ORDER BY PreguntaId;";
        using var gr = await db.QueryMultipleAsync(new CommandDefinition(sql, new { id = evaluacionId }, cancellationToken: ct, commandTimeout: 60));
        var head = await gr.ReadFirstOrDefaultAsync<(int EvaluacionId, int TiendaId, DateTime FechaCreacion)>();
        if (head.Equals(default((int, int, DateTime))))
            throw new InvalidOperationException("Evaluacion no encontrada");
        var detalles = (await gr.ReadAsync<appEvaluaciones.Shared.Models.DetalleVm>()).ToList();
        return new appEvaluaciones.Shared.Models.EvaluacionVm
        {
            EvaluacionId = head.EvaluacionId,
            TiendaId = head.TiendaId,
            FechaCreacion = head.FechaCreacion,
            Detalles = detalles
        };
    }
}
