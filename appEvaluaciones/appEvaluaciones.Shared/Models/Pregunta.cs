namespace appEvaluaciones.Shared.Models;

public sealed class Pregunta
{
    public int PreguntaId { get; set; }
    public int CategoriaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string TextoPregunta { get; set; } = string.Empty;
    public decimal Ponderacion { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

