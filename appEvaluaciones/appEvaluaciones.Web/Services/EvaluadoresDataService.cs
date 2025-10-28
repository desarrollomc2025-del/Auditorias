using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Services;

public sealed class EvaluadoresDataService(ISqlConnectionFactory factory) : IEvaluadoresService
{
    public async Task<IReadOnlyList<Evaluador>> GetAllAsync(CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        var rows = await db.QueryAsync<Evaluador>(new CommandDefinition(
            "SELECT EvaluadorId, Codigo, Nombre, Activo, FechaCreacion FROM dbo.Evaluadores WHERE Activo = 1",
            cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Evaluador?> GetByIdAsync(int evaluadorId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        return await db.QuerySingleOrDefaultAsync<Evaluador>(new CommandDefinition(
            "SELECT EvaluadorId, Codigo, Nombre, Activo, FechaCreacion FROM dbo.Evaluadores WHERE EvaluadorId = @evaluadorId",
            new { evaluadorId }, cancellationToken: ct));
    }

    public async Task<int> UpsertAsync(Evaluador evaluador, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Evaluadores WHERE EvaluadorId=@EvaluadorId)
BEGIN
    UPDATE dbo.Evaluadores SET Codigo=@Codigo, Nombre=@Nombre, Activo=@Activo
    WHERE EvaluadorId=@EvaluadorId;
    SELECT @EvaluadorId;
END
ELSE
BEGIN
    INSERT INTO dbo.Evaluadores(Codigo, Nombre, Activo, FechaCreacion)
    VALUES(@Codigo, @Nombre, @Activo, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, evaluador, cancellationToken: ct));
    }

    public async Task DeleteAsync(int evaluadorId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Evaluadores SET Activo = 0 WHERE EvaluadorId = @evaluadorId",
            new { evaluadorId }, cancellationToken: ct));
    }
}

