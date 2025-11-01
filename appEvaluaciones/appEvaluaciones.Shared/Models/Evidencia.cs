namespace appEvaluaciones.Shared.Models;

public sealed class Evidencia
{
    public int EvidenciaId { get; set; }
    public int EvaluacionId { get; set; }
    public int PreguntaId { get; set; }
    public string? Comentario { get; set; }
    public string? Url { get; set; }
    public DateTime FechaCreacion { get; set; }
}

