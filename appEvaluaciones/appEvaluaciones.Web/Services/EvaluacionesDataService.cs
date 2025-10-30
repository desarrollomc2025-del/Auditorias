using System.Data;
using Dapper;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Services;

public sealed class EvaluacionesDataService(ISqlConnectionFactory factory) : IEvaluacionesService
{
    public async Task<int> CreateAsync(Guid evaluacionKey, int tiendaId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Evaluaciones WHERE EvaluacionKey=@evaluacionKey)
BEGIN
    SELECT EvaluacionId FROM dbo.Evaluaciones WHERE EvaluacionKey=@evaluacionKey;
END
ELSE
BEGIN
    INSERT INTO dbo.Evaluaciones(EvaluacionKey, TiendaId, FechaCreacion)
    VALUES(@evaluacionKey, @tiendaId, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { evaluacionKey, tiendaId }, cancellationToken: ct, commandTimeout: 60));
    }

    public async Task UpsertDetalleAsync(Guid evaluacionKey, int preguntaId, bool? respuesta, string? comentario, decimal ponderacion, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"DECLARE @EvalId INT = (SELECT EvaluacionId FROM dbo.Evaluaciones WHERE EvaluacionKey=@evaluacionKey);
IF @EvalId IS NULL
BEGIN
    THROW 50000, 'Evaluacion no existe', 1;
END

IF EXISTS(SELECT 1 FROM dbo.DetalleEvaluaciones WHERE EvaluacionId=@EvalId AND PreguntaId=@preguntaId)
BEGIN
    UPDATE dbo.DetalleEvaluaciones
    SET Respuesta=@respuesta, Comentario=@comentario, Ponderacion=@ponderacion
    WHERE EvaluacionId=@EvalId AND PreguntaId=@preguntaId;
END
ELSE
BEGIN
    INSERT INTO dbo.DetalleEvaluaciones(EvaluacionId, PreguntaId, Respuesta, Comentario, Ponderacion)
    VALUES(@EvalId, @preguntaId, @respuesta, @comentario, @ponderacion);
END";
        await db.ExecuteAsync(new CommandDefinition(sql, new { evaluacionKey, preguntaId, respuesta, comentario, ponderacion }, cancellationToken: ct, commandTimeout: 60));
    }

    public async Task UpsertDetallesAsync(Guid evaluacionKey, IEnumerable<DetalleUpsert> detalles, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"SET XACT_ABORT ON;
DECLARE @EvalId INT = (SELECT EvaluacionId FROM dbo.Evaluaciones WHERE EvaluacionKey=@evaluacionKey);
IF @EvalId IS NULL THROW 50000, 'Evaluacion no existe', 1;

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
ON d.EvaluacionId = @EvalId AND d.PreguntaId = s.PreguntaId
WHEN MATCHED THEN
    UPDATE SET Respuesta = s.Respuesta, Comentario = s.Comentario, Ponderacion = s.Ponderacion
WHEN NOT MATCHED THEN
    INSERT (EvaluacionId, PreguntaId, Respuesta, Comentario, Ponderacion)
    VALUES (@EvalId, s.PreguntaId, s.Respuesta, s.Comentario, s.Ponderacion);";

        var payload = System.Text.Json.JsonSerializer.Serialize(detalles);
        var args = new { evaluacionKey, payload };
        await db.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct, commandTimeout: 120));
    }

    public async Task FinalizarAsync(Guid evaluacionKey, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"UPDATE dbo.Evaluaciones
SET Estado = 'Finalizada', FechaCierre = SYSUTCDATETIME()
WHERE EvaluacionKey=@evaluacionKey;";
        await db.ExecuteAsync(new CommandDefinition(sql, new { evaluacionKey }, cancellationToken: ct, commandTimeout: 30));
    }

    public async Task<appEvaluaciones.Shared.Models.EvaluacionVm> GetAsync(Guid evaluacionKey, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"SELECT TOP 1 EvaluacionId, EvaluacionKey, TiendaId, FechaCreacion
FROM dbo.Evaluaciones WHERE EvaluacionKey=@key;
SELECT PreguntaId, Respuesta, Comentario, Ponderacion
FROM dbo.DetalleEvaluaciones d
JOIN dbo.Evaluaciones e ON e.EvaluacionId = d.EvaluacionId
WHERE e.EvaluacionKey=@key
ORDER BY PreguntaId;";
        using var gr = await db.QueryMultipleAsync(new CommandDefinition(sql, new { key = evaluacionKey }, cancellationToken: ct, commandTimeout: 60));
        var head = await gr.ReadFirstOrDefaultAsync<(int EvaluacionId, Guid EvaluacionKey, int TiendaId, DateTime FechaCreacion)>();
        if (head.Equals(default((int, Guid, int, DateTime))))
            throw new InvalidOperationException("Evaluacion no encontrada");
        var detalles = (await gr.ReadAsync<appEvaluaciones.Shared.Models.DetalleVm>()).ToList();
        return new appEvaluaciones.Shared.Models.EvaluacionVm
        {
            EvaluacionId = head.EvaluacionId,
            EvaluacionKey = head.EvaluacionKey,
            TiendaId = head.TiendaId,
            FechaCreacion = head.FechaCreacion,
            Detalles = detalles
        };
    }
}
