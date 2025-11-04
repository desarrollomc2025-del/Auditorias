/*
Seed a user linked to EvaluadorId = 1 if such Evaluador exists.
User: evaluador1, Password: Eval123!, Rol: Evaluador
Idempotent: inserts only if not exists and FK target exists.
*/

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.Evaluadores WHERE EvaluadorId = 1)
       AND NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Usuario = 'evaluador1')
    BEGIN
        INSERT INTO dbo.Usuarios (Usuario, Correo, Nombre, Apellidos, Activo, IntentosFallidos, BloqueadoHasta, FechaCreacion, PasswordHash, Rol, EvaluadorId)
        VALUES ('evaluador1','evaluador1@example.local', 'Evaluador', 'Uno', 1, 0, NULL, SYSUTCDATETIME(), HASHBYTES('SHA2_256','Eval123!'), 'Evaluador', 1);
    END

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH
