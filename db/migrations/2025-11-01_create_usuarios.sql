/*
Create dbo.Usuarios to support JWT login + roles
 - UsuarioId INT IDENTITY PK
 - Username NVARCHAR(100) UNIQUE NOT NULL
 - PasswordHash VARBINARY(64) NOT NULL (SHA2_256)
 - Rol NVARCHAR(20) NOT NULL CHECK (Rol IN ('Admin','Evaluador'))
 - EvaluadorId INT NULL FK dbo.Evaluadores(EvaluadorId)
Seed one admin user (user: admin, pass: admin123!)
*/

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID('dbo.Usuarios','U') IS NULL
    BEGIN
        CREATE TABLE dbo.Usuarios (
            UsuarioId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            Username NVARCHAR(100) NOT NULL UNIQUE,
            PasswordHash VARBINARY(64) NOT NULL,
            Rol NVARCHAR(20) NOT NULL,
            EvaluadorId INT NULL,
            CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN ('Admin','Evaluador')),
            CONSTRAINT FK_Usuarios_Evaluador FOREIGN KEY (EvaluadorId) REFERENCES dbo.Evaluadores(EvaluadorId)
        );
    END

    -- Seed admin if not exists
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Username = 'admin')
    BEGIN
        INSERT INTO dbo.Usuarios (Username, PasswordHash, Rol, EvaluadorId)
        VALUES ('admin', HASHBYTES('SHA2_256','admin123!'), 'Admin', NULL);
    END

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH

