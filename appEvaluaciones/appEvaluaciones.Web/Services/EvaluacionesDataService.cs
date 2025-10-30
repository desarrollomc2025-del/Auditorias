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
}
