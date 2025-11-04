/*
Alter existing dbo.Usuarios to support JWT auth and roles.
Adds columns when missing:
 - PasswordHash VARBINARY(64) NULL (then set and make NOT NULL)
 - Rol NVARCHAR(20) NOT NULL DEFAULT 'Evaluador' with CHECK
 - EvaluadorId INT NULL FK to dbo.Evaluadores
Seeds an 'admin' user if absent (password: admin123!).
*/

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    IF COL_LENGTH('dbo.Usuarios','PasswordHash') IS NULL EXEC(N'ALTER TABLE dbo.Usuarios ADD PasswordHash VARBINARY(64) NULL;');
    IF COL_LENGTH('dbo.Usuarios','Rol') IS NULL EXEC(N'ALTER TABLE dbo.Usuarios ADD Rol NVARCHAR(20) NULL;');
    IF COL_LENGTH('dbo.Usuarios','EvaluadorId') IS NULL EXEC(N'ALTER TABLE dbo.Usuarios ADD EvaluadorId INT NULL;');

    -- Ensure CHECK constraint on Rol values (dynamic SQL to avoid compile-time resolution)
    IF OBJECT_ID('CK_Usuarios_Rol','C') IS NULL AND COL_LENGTH('dbo.Usuarios','Rol') IS NOT NULL
        EXEC(N'ALTER TABLE dbo.Usuarios ADD CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN (''Admin'',''Evaluador''));');

    -- Add FK if not exists and column present
    IF COL_LENGTH('dbo.Usuarios','EvaluadorId') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Usuarios_Evaluador' AND parent_object_id = OBJECT_ID('dbo.Usuarios'))
    BEGIN
        ALTER TABLE dbo.Usuarios ADD CONSTRAINT FK_Usuarios_Evaluador FOREIGN KEY (EvaluadorId) REFERENCES dbo.Evaluadores(EvaluadorId);
    END

    -- Seed admin (Usuario='admin')
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Usuario = 'admin')
    BEGIN
        EXEC(N'INSERT INTO dbo.Usuarios (Usuario, Correo, Nombre, Apellidos, Activo, IntentosFallidos, BloqueadoHasta, FechaCreacion, PasswordHash, Rol, EvaluadorId)
              VALUES (''admin'', '''', ''Administrador'', '''', 1, 0, NULL, SYSUTCDATETIME(), HASHBYTES(''SHA2_256'',''admin123!''), ''Admin'', NULL);');
    END
    ELSE
    BEGIN
        -- If admin exists but without new columns, set defaults
        EXEC(N'UPDATE dbo.Usuarios
                SET PasswordHash = COALESCE(PasswordHash, HASHBYTES(''SHA2_256'',''admin123!'')),
                    Rol = COALESCE(Rol, ''Admin'')
              WHERE Usuario = ''admin'';');
    END

    -- Make Rol NOT NULL with default
    IF COL_LENGTH('dbo.Usuarios','Rol') IS NOT NULL
    BEGIN
        DECLARE @dcName sysname;
        SELECT @dcName = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.Usuarios') AND c.name = 'Rol';
        IF @dcName IS NULL EXEC(N'ALTER TABLE dbo.Usuarios ADD CONSTRAINT DF_Usuarios_Rol DEFAULT (''Evaluador'') FOR Rol;');
        EXEC(N'ALTER TABLE dbo.Usuarios ALTER COLUMN Rol NVARCHAR(20) NOT NULL;');
    END

    -- Finally, enforce PasswordHash NOT NULL (after seed)
    IF COL_LENGTH('dbo.Usuarios','PasswordHash') IS NOT NULL EXEC(N'ALTER TABLE dbo.Usuarios ALTER COLUMN PasswordHash VARBINARY(64) NOT NULL;');

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH
