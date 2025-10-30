namespace appEvaluaciones.Shared.Services;

public sealed class DetalleUpsert
{
    public int PreguntaId { get; set; }
    public bool? Respuesta { get; set; }
    public string? Comentario { get; set; }
    public decimal Ponderacion { get; set; }
}

