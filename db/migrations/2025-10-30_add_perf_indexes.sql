/*
Performance indexes to avoid timeouts on common joins and lookups.
 - UNIQUE INDEX on dbo.DetalleEvaluaciones(EvaluacionId, PreguntaId)
 - NONCLUSTERED INDEX on dbo.Evidencias(DetalleId) [if not already created]
 - Ensure UNIQUE INDEX on dbo.Evaluaciones(EvaluacionKey) (created in prior migration)
Idempotent, safe to rerun.
*/

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    -- DetalleEvaluaciones(EvaluacionId, PreguntaId)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes WHERE name = 'IX_DetalleEvaluaciones_EvaluacionId_PreguntaId'
          AND object_id = OBJECT_ID('dbo.DetalleEvaluaciones')
    )
    BEGIN
        CREATE UNIQUE INDEX IX_DetalleEvaluaciones_EvaluacionId_PreguntaId
            ON dbo.DetalleEvaluaciones(EvaluacionId, PreguntaId);
    END

    -- Evidencias(DetalleId)
    IF OBJECT_ID('dbo.Evidencias', 'U') IS NOT NULL
       AND COL_LENGTH('dbo.Evidencias', 'DetalleId') IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM sys.indexes WHERE name = 'IX_Evidencias_DetalleId'
             AND object_id = OBJECT_ID('dbo.Evidencias')
       )
    BEGIN
        CREATE INDEX IX_Evidencias_DetalleId ON dbo.Evidencias(DetalleId);
    END

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH

