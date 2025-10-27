namespace appEvaluaciones.Shared.Models;

public sealed class Empresa
{
    public int EmpresaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public bool Eliminado { get; set; }
    public DateTime FechaCreacion { get; set; }
}

