/*
Make dbo.Evaluaciones.EvaluadorId nullable and remove DEFAULT (0) to avoid FK conflicts
until authentication maps the current user to a valid EvaluadorId. Idempotent.
*/

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    IF COL_LENGTH('dbo.Evaluaciones', 'EvaluadorId') IS NOT NULL
    BEGIN
        -- Drop default constraint if any
        DECLARE @dcName sysname;
        SELECT @dcName = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c
            ON c.object_id = d.parent_object_id
           AND c.column_id = d.parent_column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.Evaluaciones')
          AND c.name = 'EvaluadorId';

        IF @dcName IS NOT NULL
        BEGIN
            DECLARE @sql NVARCHAR(MAX) = N'ALTER TABLE dbo.Evaluaciones DROP CONSTRAINT ' + QUOTENAME(@dcName) + ';';
            EXEC(@sql);
        END

        -- Normalize any 0 values to NULL to satisfy FK
        IF EXISTS (SELECT 1 FROM dbo.Evaluaciones WHERE EvaluadorId = 0)
        BEGIN
            UPDATE dbo.Evaluaciones SET EvaluadorId = NULL WHERE EvaluadorId = 0;
        END

        -- Alter column to NULL if currently NOT NULL
        IF EXISTS (
            SELECT 1 FROM sys.columns 
            WHERE object_id = OBJECT_ID('dbo.Evaluaciones') 
              AND name = 'EvaluadorId'
              AND is_nullable = 0)
        BEGIN
            ALTER TABLE dbo.Evaluaciones ALTER COLUMN EvaluadorId INT NULL;
        END
    END

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH
