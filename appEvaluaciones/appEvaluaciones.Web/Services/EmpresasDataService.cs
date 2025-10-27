using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Services;

public sealed class EmpresasDataService(ISqlConnectionFactory factory) : IEmpresasService
{
    public async Task<IReadOnlyList<Empresa>> GetAllAsync(CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        var rows = await db.QueryAsync<Empresa>(new CommandDefinition(
            "SELECT EmpresaId, Codigo, Nombre, Direccion, Eliminado, FechaCreacion FROM dbo.Empresas WHERE Eliminado = 0",
            cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Empresa?> GetByIdAsync(int empresaId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        return await db.QuerySingleOrDefaultAsync<Empresa>(new CommandDefinition(
            "SELECT EmpresaId, Codigo, Nombre, Direccion, Eliminado, FechaCreacion FROM dbo.Empresas WHERE EmpresaId = @empresaId",
            new { empresaId }, cancellationToken: ct));
    }

    public async Task<int> UpsertAsync(Empresa empresa, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Empresas WHERE EmpresaId=@EmpresaId)
BEGIN
    -- No permitir cambiar el Código al actualizar
    UPDATE dbo.Empresas SET Nombre=@Nombre, Direccion=@Direccion, Eliminado=@Eliminado
    WHERE EmpresaId=@EmpresaId;
    SELECT @EmpresaId;
END
ELSE
BEGIN
    -- Generar Código incremental automáticamente al insertar
    DECLARE @NuevoCodigo VARCHAR(50);
    SELECT @NuevoCodigo = CAST(ISNULL(MAX(TRY_CAST(Codigo AS INT)), 0) + 1 AS VARCHAR(50))
    FROM dbo.Empresas WITH (UPDLOCK, HOLDLOCK);

    INSERT INTO dbo.Empresas(Codigo, Nombre, Direccion, Eliminado, FechaCreacion)
    VALUES(@NuevoCodigo, @Nombre, @Direccion, @Eliminado, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, empresa, cancellationToken: ct));
    }

    public async Task DeleteAsync(int empresaId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Empresas SET Eliminado = 1 WHERE EmpresaId = @empresaId",
            new { empresaId }, cancellationToken: ct));
    }
}

