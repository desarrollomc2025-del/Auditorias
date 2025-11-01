namespace appEvaluaciones.Shared.Models;

public sealed class DetalleVm
{
    public int PreguntaId { get; set; }
    public bool? Respuesta { get; set; }
    public string? Comentario { get; set; }
    public decimal Ponderacion { get; set; }
}

public sealed class EvaluacionVm
{
    public int EvaluacionId { get; set; }
    public int TiendaId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<DetalleVm> Detalles { get; set; } = new();
}
