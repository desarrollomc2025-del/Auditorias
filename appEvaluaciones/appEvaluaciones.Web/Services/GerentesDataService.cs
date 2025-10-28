using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Services;

public sealed class GerentesDataService(ISqlConnectionFactory factory) : IGerentesService
{
    public async Task<IReadOnlyList<Gerente>> GetAllAsync(CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        var rows = await db.QueryAsync<Gerente>(new CommandDefinition(
            "SELECT GerenteId, Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion FROM dbo.Gerentes WHERE Activo = 1",
            cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Gerente?> GetByIdAsync(int gerenteId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        return await db.QuerySingleOrDefaultAsync<Gerente>(new CommandDefinition(
            "SELECT GerenteId, Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion FROM dbo.Gerentes WHERE GerenteId = @gerenteId",
            new { gerenteId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Gerente>> GetRegionalesAsync(CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        var rows = await db.QueryAsync<Gerente>(new CommandDefinition(
            "SELECT GerenteId, Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion FROM dbo.Gerentes WHERE Activo = 1 AND Rol = 'Regional'",
            cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<int> UpsertAsync(Gerente gerente, CancellationToken ct = default)
    {
        if (string.Equals(gerente.Rol, "Regional", StringComparison.OrdinalIgnoreCase))
            gerente.GerenteRegionalId = null;

        using IDbConnection db = factory.Create();
        const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Gerentes WHERE GerenteId=@GerenteId)
BEGIN
    UPDATE dbo.Gerentes SET Codigo=@Codigo, Nombre=@Nombre, Rol=@Rol, GerenteRegionalId=@GerenteRegionalId, Activo=@Activo
    WHERE GerenteId=@GerenteId;
    SELECT @GerenteId;
END
ELSE
BEGIN
    INSERT INTO dbo.Gerentes(Codigo, Nombre, Rol, GerenteRegionalId, Activo, FechaCreacion)
    VALUES(@Codigo, @Nombre, @Rol, @GerenteRegionalId, @Activo, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, gerente, cancellationToken: ct));
    }

    public async Task DeleteAsync(int gerenteId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Gerentes SET Activo = 0 WHERE GerenteId = @gerenteId",
            new { gerenteId }, cancellationToken: ct));
    }
}

