namespace appEvaluaciones.Shared.Models;

public sealed class TipoTienda
{
    public int TipoTiendaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}

