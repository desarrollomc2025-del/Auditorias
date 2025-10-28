namespace appEvaluaciones.Shared.Models;

public sealed class Categoria
{
    public int CategoriaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Ponderacion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

