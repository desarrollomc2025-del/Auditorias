namespace appEvaluaciones.Shared.Models;

public sealed class Tienda
{
    public int TiendaId { get; set; }
    public int EmpresaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? CodigoInterno { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int TipoTiendaId { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public bool Eliminado { get; set; }
    public DateTime FechaCreacion { get; set; }
}
