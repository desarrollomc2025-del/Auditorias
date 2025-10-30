SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    IF COL_LENGTH('dbo.Evaluaciones', 'Estado') IS NULL
    BEGIN
        ALTER TABLE dbo.Evaluaciones ADD Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Evaluaciones_Estado DEFAULT('Borrador');
    END

    IF COL_LENGTH('dbo.Evaluaciones', 'FechaCierre') IS NULL
    BEGIN
        ALTER TABLE dbo.Evaluaciones ADD FechaCierre DATETIME2 NULL;
    END

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH

