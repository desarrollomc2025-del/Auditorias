namespace appEvaluaciones.Shared.Models;

public sealed class Evaluador
{
    public int EvaluadorId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

