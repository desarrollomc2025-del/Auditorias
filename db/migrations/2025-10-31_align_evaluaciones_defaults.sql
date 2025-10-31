/*
Align existing dbo.Evaluaciones schema with app inserts by adding sensible DEFAULTs
for NOT NULL columns commonly present in legacy schemas. Idempotent and safe to rerun.

Adds defaults when the column exists and has no default:
 - EvaluadorId INT NOT NULL            -> DEFAULT (0)
 - Codigo NVARCHAR(50) NOT NULL        -> DEFAULT (CONVERT(NVARCHAR(36), NEWID()))
 - Fecha DATETIME2 NOT NULL            -> DEFAULT (SYSUTCDATETIME())
 - Estado NVARCHAR(20) NOT NULL        -> DEFAULT ('Borrador')
 - FechaCreacion DATETIME2 NOT NULL    -> DEFAULT (SYSUTCDATETIME())
*/

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    -- Helper: check if a column has a default constraint
    -- We will inline the checks with EXISTS against sys.default_constraints

    -- EvaluadorId default
    IF COL_LENGTH('dbo.Evaluaciones', 'EvaluadorId') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID('dbo.Evaluaciones')
              AND c.name = 'EvaluadorId')
    BEGIN
        ALTER TABLE dbo.Evaluaciones
        ADD CONSTRAINT DF_Evaluaciones_EvaluadorId DEFAULT (0) FOR EvaluadorId;
    END

    -- Codigo default
    IF COL_LENGTH('dbo.Evaluaciones', 'Codigo') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID('dbo.Evaluaciones')
              AND c.name = 'Codigo')
    BEGIN
        ALTER TABLE dbo.Evaluaciones
        ADD CONSTRAINT DF_Evaluaciones_Codigo DEFAULT (CONVERT(NVARCHAR(36), NEWID())) FOR Codigo;
    END

    -- Fecha default
    IF COL_LENGTH('dbo.Evaluaciones', 'Fecha') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID('dbo.Evaluaciones')
              AND c.name = 'Fecha')
    BEGIN
        ALTER TABLE dbo.Evaluaciones
        ADD CONSTRAINT DF_Evaluaciones_Fecha DEFAULT (SYSUTCDATETIME()) FOR Fecha;
    END

    -- Estado default
    IF COL_LENGTH('dbo.Evaluaciones', 'Estado') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID('dbo.Evaluaciones')
              AND c.name = 'Estado')
    BEGIN
        ALTER TABLE dbo.Evaluaciones
        ADD CONSTRAINT DF_Evaluaciones_Estado DEFAULT ('Borrador') FOR Estado;
    END

    -- FechaCreacion default
    IF COL_LENGTH('dbo.Evaluaciones', 'FechaCreacion') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID('dbo.Evaluaciones')
              AND c.name = 'FechaCreacion')
    BEGIN
        ALTER TABLE dbo.Evaluaciones
        ADD CONSTRAINT DF_Evaluaciones_FechaCreacion DEFAULT (SYSUTCDATETIME()) FOR FechaCreacion;
    END

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH

