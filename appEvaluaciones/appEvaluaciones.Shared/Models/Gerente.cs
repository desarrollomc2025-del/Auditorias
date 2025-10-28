namespace appEvaluaciones.Shared.Models;

public sealed class Gerente
{
    public int GerenteId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty; // 'Regional' | 'Area'
    public int? GerenteRegionalId { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

