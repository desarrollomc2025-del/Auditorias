/*
Ensure dbo.Evaluaciones.EvaluacionKey has a DEFAULT NEWID() so inserts that don't specify it succeed.
Idempotent: only adds default if column exists and no default is present.
*/

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    IF COL_LENGTH('dbo.Evaluaciones', 'EvaluacionKey') IS NOT NULL
    BEGIN
        DECLARE @dcName sysname;
        SELECT @dcName = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c
            ON c.object_id = d.parent_object_id
           AND c.column_id = d.parent_column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.Evaluaciones')
          AND c.name = 'EvaluacionKey';

        IF @dcName IS NULL
        BEGIN
            ALTER TABLE dbo.Evaluaciones
                ADD CONSTRAINT DF_Evaluaciones_EvaluacionKey
                DEFAULT (NEWID()) FOR EvaluacionKey;
        END
    END

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH

