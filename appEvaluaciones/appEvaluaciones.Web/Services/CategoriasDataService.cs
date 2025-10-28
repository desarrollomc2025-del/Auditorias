using System.Data;
using Dapper;
using appEvaluaciones.Shared.Models;
using appEvaluaciones.Shared.Services;

namespace appEvaluaciones.Web.Services;

public sealed class CategoriasDataService(ISqlConnectionFactory factory) : ICategoriasService
{
    public async Task<IReadOnlyList<Categoria>> GetAllAsync(CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        var rows = await db.QueryAsync<Categoria>(new CommandDefinition(
            "SELECT CategoriaId, Codigo, Descripcion, Ponderacion, Activo, FechaCreacion FROM dbo.Categorias WHERE Activo = 1",
            cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Categoria?> GetByIdAsync(int categoriaId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        return await db.QuerySingleOrDefaultAsync<Categoria>(new CommandDefinition(
            "SELECT CategoriaId, Codigo, Descripcion, Ponderacion, Activo, FechaCreacion FROM dbo.Categorias WHERE CategoriaId = @categoriaId",
            new { categoriaId }, cancellationToken: ct));
    }

    public async Task<int> UpsertAsync(Categoria categoria, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        const string sql = @"IF EXISTS(SELECT 1 FROM dbo.Categorias WHERE CategoriaId=@CategoriaId)
BEGIN
    UPDATE dbo.Categorias SET Codigo=@Codigo, Descripcion=@Descripcion, Ponderacion=@Ponderacion, Activo=@Activo
    WHERE CategoriaId=@CategoriaId;
    SELECT @CategoriaId;
END
ELSE
BEGIN
    INSERT INTO dbo.Categorias(Codigo, Descripcion, Ponderacion, Activo, FechaCreacion)
    VALUES(@Codigo, @Descripcion, @Ponderacion, @Activo, SYSUTCDATETIME());
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END";
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, categoria, cancellationToken: ct));
    }

    public async Task DeleteAsync(int categoriaId, CancellationToken ct = default)
    {
        using IDbConnection db = factory.Create();
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.Categorias SET Activo = 0 WHERE CategoriaId = @categoriaId",
            new { categoriaId }, cancellationToken: ct));
    }
}

