/*
Idempotent migration to align DB with app schema.
Adds:
 - dbo.Evaluaciones.EvaluacionKey (UNIQUEIDENTIFIER, NOT NULL, UNIQUE)
 - dbo.DetalleEvaluaciones.Respuesta (BIT, NULL)
 - dbo.DetalleEvaluaciones.Comentario (NVARCHAR(500), NULL)
 - dbo.DetalleEvaluaciones.Ponderacion (DECIMAL(10,2), NOT NULL DEFAULT 0)

Safeguards:
 - Uses COL_LENGTH to check existence
 - Backfills EvaluacionKey for existing rows
 - If a legacy column 'Cumple' exists, copies its values into Respuesta
*/

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    -- Evaluaciones.EvaluacionKey
    IF COL_LENGTH('dbo.Evaluaciones', 'EvaluacionKey') IS NULL
    BEGIN
        ALTER TABLE dbo.Evaluaciones
        ADD EvaluacionKey UNIQUEIDENTIFIER NULL; -- add as NULLable first to allow backfill

        -- Backfill existing rows
        UPDATE dbo.Evaluaciones
        SET EvaluacionKey = NEWID()
        WHERE EvaluacionKey IS NULL;

        -- Make NOT NULL and add unique index
        ALTER TABLE dbo.Evaluaciones
        ALTER COLUMN EvaluacionKey UNIQUEIDENTIFIER NOT NULL;

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = 'IX_Evaluaciones_EvaluacionKey' AND object_id = OBJECT_ID('dbo.Evaluaciones')
        )
        BEGIN
            CREATE UNIQUE INDEX IX_Evaluaciones_EvaluacionKey
                ON dbo.Evaluaciones(EvaluacionKey);
        END
    END

    -- DetalleEvaluaciones.Respuesta
    IF COL_LENGTH('dbo.DetalleEvaluaciones', 'Respuesta') IS NULL
    BEGIN
        ALTER TABLE dbo.DetalleEvaluaciones
        ADD Respuesta BIT NULL;

        -- Optional backfill from legacy 'Cumple'
        IF COL_LENGTH('dbo.DetalleEvaluaciones', 'Cumple') IS NOT NULL
        BEGIN
            UPDATE d
            SET d.Respuesta = d.Cumple
            FROM dbo.DetalleEvaluaciones d
            WHERE d.Respuesta IS NULL;
        END
    END

    -- DetalleEvaluaciones.Comentario
    IF COL_LENGTH('dbo.DetalleEvaluaciones', 'Comentario') IS NULL
    BEGIN
        ALTER TABLE dbo.DetalleEvaluaciones
        ADD Comentario NVARCHAR(500) NULL;
    END

    -- DetalleEvaluaciones.Ponderacion
    IF COL_LENGTH('dbo.DetalleEvaluaciones', 'Ponderacion') IS NULL
    BEGIN
        ALTER TABLE dbo.DetalleEvaluaciones
        ADD Ponderacion DECIMAL(10,2) NOT NULL CONSTRAINT DF_DetalleEvaluaciones_Ponderacion DEFAULT (0);
    END

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH

