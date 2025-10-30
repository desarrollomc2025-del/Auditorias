/*
Align dbo.Evidencias schema with application expectations.
Adds/creates columns:
 - EvidenciaId (INT IDENTITY, PK)
 - DetalleId (INT, FK -> dbo.DetalleEvaluaciones.DetalleId)
 - Descripcion NVARCHAR(500) NULL
 - UrlArchivo NVARCHAR(500) NULL
 - NombreArchivo NVARCHAR(255) NULL
 - FechaCreacion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()

Backfill (optional) from legacy columns when present:
 - If dbo.Evidencias has EvaluacionKey (UNIQUEIDENTIFIER) and PreguntaId (INT), compute DetalleId by join.
 - If dbo.Evidencias has Comentario, copy to Descripcion when Descripcion is NULL/empty.
 - If dbo.Evidencias has Url, copy to UrlArchivo when UrlArchivo is NULL/empty.

Idempotent and safe to rerun.
*/

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    -- Create table if missing
    IF OBJECT_ID('dbo.Evidencias', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Evidencias
        (
            EvidenciaId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Evidencias PRIMARY KEY,
            DetalleId     INT NOT NULL,
            Descripcion   NVARCHAR(500) NULL,
            UrlArchivo    NVARCHAR(500) NULL,
            NombreArchivo NVARCHAR(255) NULL,
            FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Evidencias_FechaCreacion DEFAULT SYSUTCDATETIME()
        );

        IF NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Evidencias_DetalleEvaluaciones_DetalleId'
        )
        BEGIN
            ALTER TABLE dbo.Evidencias
            ADD CONSTRAINT FK_Evidencias_DetalleEvaluaciones_DetalleId
                FOREIGN KEY (DetalleId) REFERENCES dbo.DetalleEvaluaciones(DetalleId);
        END

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes WHERE name = 'IX_Evidencias_DetalleId' AND object_id = OBJECT_ID('dbo.Evidencias')
        )
        BEGIN
            CREATE INDEX IX_Evidencias_DetalleId ON dbo.Evidencias(DetalleId);
        END
    END
    ELSE
    BEGIN
        -- Add columns if missing
        IF COL_LENGTH('dbo.Evidencias', 'DetalleId') IS NULL
        BEGIN
            ALTER TABLE dbo.Evidencias ADD DetalleId INT NULL; -- nullable for backfill
        END
        IF COL_LENGTH('dbo.Evidencias', 'Descripcion') IS NULL
        BEGIN
            ALTER TABLE dbo.Evidencias ADD Descripcion NVARCHAR(500) NULL;
        END
        IF COL_LENGTH('dbo.Evidencias', 'UrlArchivo') IS NULL
        BEGIN
            ALTER TABLE dbo.Evidencias ADD UrlArchivo NVARCHAR(500) NULL;
        END
        IF COL_LENGTH('dbo.Evidencias', 'NombreArchivo') IS NULL
        BEGIN
            ALTER TABLE dbo.Evidencias ADD NombreArchivo NVARCHAR(255) NULL;
        END
        IF COL_LENGTH('dbo.Evidencias', 'FechaCreacion') IS NULL
        BEGIN
            ALTER TABLE dbo.Evidencias ADD FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Evidencias_FechaCreacion DEFAULT SYSUTCDATETIME();
        END

        -- Backfill DetalleId using EvaluacionKey + PreguntaId if present
        IF COL_LENGTH('dbo.Evidencias', 'DetalleId') IS NOT NULL
           AND COL_LENGTH('dbo.Evidencias', 'EvaluacionKey') IS NOT NULL
           AND COL_LENGTH('dbo.Evidencias', 'PreguntaId') IS NOT NULL
        BEGIN
            EXEC(N'UPDATE e
                   SET e.DetalleId = d.DetalleId
                   FROM dbo.Evidencias e
                   JOIN dbo.Evaluaciones ev ON ev.EvaluacionKey = e.EvaluacionKey
                   JOIN dbo.DetalleEvaluaciones d ON d.EvaluacionId = ev.EvaluacionId AND d.PreguntaId = e.PreguntaId
                   WHERE e.DetalleId IS NULL;');
        END

        -- Backfill Descripcion from Comentario if present
        IF COL_LENGTH('dbo.Evidencias', 'Descripcion') IS NOT NULL
           AND COL_LENGTH('dbo.Evidencias', 'Comentario') IS NOT NULL
        BEGIN
            EXEC(N'UPDATE e
                   SET e.Descripcion = COALESCE(NULLIF(e.Descripcion, ''''), e.Comentario)
                   FROM dbo.Evidencias e
                   WHERE e.Comentario IS NOT NULL AND (e.Descripcion IS NULL OR e.Descripcion = '''');');
        END

        -- Backfill UrlArchivo from Url if present
        IF COL_LENGTH('dbo.Evidencias', 'UrlArchivo') IS NOT NULL
           AND COL_LENGTH('dbo.Evidencias', 'Url') IS NOT NULL
        BEGIN
            EXEC(N'UPDATE e
                   SET e.UrlArchivo = COALESCE(NULLIF(e.UrlArchivo, ''''), e.Url)
                   FROM dbo.Evidencias e
                   WHERE e.Url IS NOT NULL AND (e.UrlArchivo IS NULL OR e.UrlArchivo = '''');');
        END

        -- Add FK if missing
        IF NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Evidencias_DetalleEvaluaciones_DetalleId'
        ) AND COL_LENGTH('dbo.Evidencias', 'DetalleId') IS NOT NULL
        BEGIN
            ALTER TABLE dbo.Evidencias
            ADD CONSTRAINT FK_Evidencias_DetalleEvaluaciones_DetalleId
                FOREIGN KEY (DetalleId) REFERENCES dbo.DetalleEvaluaciones(DetalleId);
        END

        -- Index on DetalleId
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes WHERE name = 'IX_Evidencias_DetalleId' AND object_id = OBJECT_ID('dbo.Evidencias')
        ) AND COL_LENGTH('dbo.Evidencias', 'DetalleId') IS NOT NULL
        BEGIN
            CREATE INDEX IX_Evidencias_DetalleId ON dbo.Evidencias(DetalleId);
        END

        -- Make DetalleId NOT NULL if there are no NULLs
        IF COL_LENGTH('dbo.Evidencias', 'DetalleId') IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.Evidencias WHERE DetalleId IS NULL)
        BEGIN
            EXEC(N'ALTER TABLE dbo.Evidencias ALTER COLUMN DetalleId INT NOT NULL;');
        END
    END

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH

